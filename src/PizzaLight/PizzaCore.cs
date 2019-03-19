using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Noobot.Core.MessagingPipeline.Request;
using Noobot.Core.MessagingPipeline.Response;
using PizzaLight.Infrastructure;
using PizzaLight.NoobotInternals;
using PizzaLight.Resources;
using Serilog;
using SlackConnector;
using SlackConnector.Models;
using StructureMap.TypeRules;

namespace PizzaLight
{
    public class PizzaCore : IPizzaCore
    {
        private readonly BotConfig _botConfig;
        private readonly Serilog.ILogger _logger;
        private readonly List<IMessageHandler> _messageHandlers = new List<IMessageHandler>();
        private bool _isDisconnecting = false;
        /// <summary>
        /// Can be null if disconnected. Should wait for reconnection.
        /// </summary>
        public ISlackConnection SlackConnection { get; private set; }

        public IReadOnlyDictionary<string, SlackUser> UserCache => SlackConnection.UserCache;

        public PizzaCore(BotConfig botConfig, ILogger logger)
        {
            _botConfig = botConfig ?? throw new ArgumentNullException(nameof(botConfig));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task Start()
        {
            _logger.Information("Starting PizzaCore");
            await CreateConnection();
            _logger.Information("PizzaCore finished starting up.");
        }

        private async Task CreateConnection()
        {
            _logger.Information("Connecting...");
            var task = ConnectToSlack();
            await task.ContinueWith(t =>
                {
                    if (!task.IsCompleted || task.IsFaulted || task.IsCanceled)
                    {
                        _logger.Warning($"Error connecting to Slack: {task.Exception}");
                    }
                }
            );
        }

        private async Task ConnectToSlack()
        {
            SlackConnection = await new SlackConnector.SlackConnector().Connect(_botConfig.SlackApiKey);
            SlackConnection.OnMessageReceived += MessageReceived;
            SlackConnection.OnDisconnect += OnDisconnect;
            SlackConnection.OnReconnecting += OnReconnecting;
            SlackConnection.OnReconnect += OnReconnect;

            _logger.Information("Connected!");
            _logger.Information($"Bots Name: {SlackConnection.Self.Name}");
            _logger.Information($"Team Name: {SlackConnection.Team.Name}");
        }

        public void Stop()
        {
            Console.WriteLine("Disconnecting from slack...");
            Disconnect();
        }

        public void AddMessageHandlerToPipeline(IMessageHandler handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            if (_messageHandlers.Any(h => h.GetType().CanBeCastTo(handler.GetType())))
            {
                throw new InvalidOperationException($"Handler of type {handler.GetType().Name} already exists in message handlers.");
            }
            _messageHandlers.Add(handler);
        }

        private Task OnReconnect()
        {
            _logger.Information("SlackConnection Restored!");
            //_container.GetPlugin<StatsPlugin>().IncrementState("ConnectionsRestored");
            return Task.CompletedTask;
        }

        private Task OnReconnecting()
        {
            _logger.Information("Attempting to reconnect to Slack...");
            //_container.GetPlugin<StatsPlugin>().IncrementState("Reconnecting");
            return Task.CompletedTask;
        }

        private void Disconnect()
        {
            _isDisconnecting = true;

            if (SlackConnection != null && SlackConnection.IsConnected)
            {
                SlackConnection
                    .Close()
                    .GetAwaiter()
                    .GetResult();
            }
        }

        private void OnDisconnect()
        {
            //StopPlugins();

            if (_isDisconnecting)
            {
                _logger.Information("Disconnected.");
            }
            else
            {
                _logger.Information("Disconnected from server, attempting to reconnect...");
                Reconnect();
            }
        }

        internal void Reconnect()
        {
            _logger.Information("Reconnecting...");
            if (SlackConnection != null)
            {
                SlackConnection.OnMessageReceived -= MessageReceived;
                SlackConnection.OnDisconnect -= OnDisconnect;
                SlackConnection = null;
            }

            _isDisconnecting = false;
            ConnectToSlack()
                .ContinueWith(task =>
                {
                    if (task.IsCompleted && !task.IsCanceled && !task.IsFaulted)
                    {
                        _logger.Information("SlackConnection restored.");
                        //_container.GetPlugin<StatsPlugin>().IncrementState("ConnectionsRestored");
                    }
                    else
                    {
                        _logger.Information($"Error while reconnecting: {task.Exception}");
                    }
                })
                .Wait();
        }

        public async Task MessageReceived(SlackMessage message)
        {
            _logger.Information("[Message found from '{FromUserName}']", message.User.Name);
            _logger.Debug(message.Text);

            var incomingMessage = new IncomingMessage
            {
                RawText = message.Text,
                FullText = message.Text,
                UserId = message.User.Id,
                Username = GetUsername(message),
                UserEmail = message.User.Email,
                Channel = message.ChatHub.Id,
                ChannelType = message.ChatHub.Type == SlackChatHubType.DM ? ResponseType.DirectMessage : ResponseType.Channel,
                UserChannel = await GetUserChannel(message),
                BotName = SlackConnection.Self.Name,
                BotId = SlackConnection.Self.Id,
                BotIsMentioned = message.MentionsBot
            };

            incomingMessage.TargetedText = incomingMessage.GetTargetedText();

            if (message.ChatHub.Type != SlackChatHubType.DM)
            {
                return;
            }
            foreach (var resource in _messageHandlers)
            {
                await resource.HandleMessage(incomingMessage);
            }

        }

        public async Task SendMessage(ResponseMessage responseMessage)
        {
            SlackChatHub chatHub = await GetChatHub(responseMessage);

            if (chatHub != null)
            {
                if (responseMessage is TypingIndicatorMessage)
                {
                    _logger.Information($"Indicating typing on channel '{chatHub.Name}'");
                    await SlackConnection.IndicateTyping(chatHub);
                }
                else
                {
                    var botMessage = new BotMessage
                    {
                        ChatHub = chatHub,
                        Text = responseMessage.Text,
                        //Attachments = GetAttachments(responseMessage.Attachments) 
                    };

                    string textTrimmed = botMessage.Text.Length > 50 ? botMessage.Text.Substring(0, 50) + "..." : botMessage.Text;
                    _logger.Information($"Sending message '{textTrimmed}'");
                    await SlackConnection.Say(botMessage);
                }
            }
            else
            {
                _logger.Error($"Unable to find channel for message '{responseMessage.Text}'. Message not sent");
            }
        }

        //private IList<SlackAttachment> GetAttachments(List<Attachment> attachments)
        //{
        //    var slackAttachments = new List<SlackAttachment>();

        //    if (attachments != null)
        //    {
        //        foreach (var attachment in attachments)
        //        {
        //            slackAttachments.Add(new SlackAttachment
        //            {
        //                Text = attachment.Text,
        //                Title = attachment.Title,
        //                Fallback = attachment.Fallback,
        //                ImageUrl = attachment.ImageUrl,
        //                ThumbUrl = attachment.ThumbUrl,
        //                AuthorName = attachment.AuthorName,
        //                ColorHex = attachment.Color,
        //                Fields = GetAttachmentFields(attachment)
        //            });
        //        }
        //    }
        //    return slackAttachments;
        //}
        //private IList<SlackAttachmentField> GetAttachmentFields(Attachment attachment)
        //{
        //    var attachmentFields = new List<SlackAttachmentField>();

        //    if (attachment?.AttachmentFields != null)
        //    {
        //        foreach (var attachmentField in attachment.AttachmentFields)
        //        {
        //            attachmentFields.Add(new SlackAttachmentField
        //            {
        //                Title = attachmentField.Title,
        //                Value = attachmentField.Value,
        //                IsShort = attachmentField.IsShort
        //            });
        //        }
        //    }

        //    return attachmentFields;
        //}



        private string GetUsername(SlackMessage message)
        {
            return UserCache.ContainsKey(message.User.Id) ? UserCache[message.User.Id].Name : string.Empty;
        }
        private async Task<string> GetUserChannel(SlackMessage message)
        {
            return (await GetUserChatHub(message.User.Id, joinChannel: false) ?? new SlackChatHub()).Id;
        }

        private async Task<SlackChatHub> GetChatHub(ResponseMessage responseMessage)
        {
            SlackChatHub chatHub = null;

            if (responseMessage.ResponseType == ResponseType.Channel)
            {
                chatHub = new SlackChatHub { Id = responseMessage.Channel };
            }
            else if (responseMessage.ResponseType == ResponseType.DirectMessage)
            {
                if (string.IsNullOrEmpty(responseMessage.Channel))
                {
                    chatHub = await GetUserChatHub(responseMessage.UserId);
                }
                else
                {
                    chatHub = new SlackChatHub { Id = responseMessage.Channel };
                }
            }

            return chatHub;
        }

        private async Task<SlackChatHub> GetUserChatHub(string userId, bool joinChannel = true)
        {
            SlackChatHub chatHub = null;

            if (UserCache.ContainsKey(userId))
            {
                string username = "@" + UserCache[userId].Name;
                chatHub = SlackConnection.ConnectedDMs().FirstOrDefault(x => x.Name.Equals(username, StringComparison.OrdinalIgnoreCase));
            }

            if (chatHub == null && joinChannel)
            {
                chatHub = await SlackConnection.JoinDirectMessageChannel(userId);
            }

            return chatHub;
        }


    }
}