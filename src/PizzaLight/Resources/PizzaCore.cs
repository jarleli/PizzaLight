using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PizzaLight.Infrastructure;
using PizzaLight.Models.SlackModels;
using PizzaLight.Resources.ExtensionClasses;
using Serilog;
using SlackAPI;
using SlackAPI.WebSocketMessages;

namespace PizzaLight.Resources
{
    public class PizzaCore : IPizzaCore
    {
        private readonly BotConfig _botConfig;
        private readonly Serilog.ILogger _logger;
        private readonly IActivityLog _activityLog;
        private readonly List<IMessageHandler> _messageHandlers = new List<IMessageHandler>();
        private bool _isDisconnecting = false;

        SlackTaskClient Client { get; set; }
        private SlackSocketClient SocketClient { get; set; }

        public List<User> UserCache => SocketClient.Users;
        public List<Channel> Channels => SocketClient.Channels;
        public bool IsConnected => SocketClient.IsConnected;

        public PizzaCore(BotConfig botConfig, ILogger logger)
        {
            _botConfig = botConfig ?? throw new ArgumentNullException(nameof(botConfig));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            Client = new SlackTaskClient(_botConfig.SlackApiKey);
            SocketClient = new SlackSocketClient(_botConfig.SlackApiKey);
        }

        public async Task Start()
        {
            _logger.Debug("Starting PizzaCore");
            await CreateConnection();
            _logger.Information("PizzaCore finished starting up.");
        }

        private async Task CreateConnection()
        {
            _logger.Debug("Connecting...");
            var task = ConnectToSlack();
            await task.ContinueWith(t =>
                {
                    if (!task.IsCompleted || task.IsFaulted || task.IsCanceled)
                    {
                        _logger.Error($"Error connecting to Slack: {task.Exception}");
                    }
                }
            );
        }

        private async Task ConnectToSlack()
        {
            await Client.ConnectAsync();
            _logger.Verbose($"Team Name: {Client.MyTeam.name}");
            _logger.Verbose($"Bots Name: {Client.MySelf.name}");

            await ConnectSocket();
        }

        private async Task ConnectSocket()
        {
            var wait = new ManualResetEventSlim();
            Task t = new Task(()=>
            {
                SocketClient.OnMessageReceived += OnMessageReceived;
                SocketClient.OnConnectionLost += OnDisconnect;
                SocketClient.OnHello += OnSocketInitialized;
                _logger.Information("Socket Connected!");
            });
            SocketClient.Connect(null, () => { t.Start(); });
            await t;
        }

        public async Task Stop()
        {
            Console.WriteLine("Disconnecting from slack...");
            await Disconnect();
        }

        public void AddMessageHandlerToPipeline(params IMessageHandler[] messageHandlers)
        {
            if (messageHandlers == null) throw new ArgumentNullException(nameof(messageHandlers));
            foreach (var handler in messageHandlers)
            {
                if (_messageHandlers.Any(h => h.GetType() == handler.GetType()))
                {
                    throw new InvalidOperationException($"Handler of type {handler.GetType().Name} already exists in message handlers.");
                }
                _messageHandlers.Add(handler);
            }
        }

        public void OnMessageReceived(NewMessage message)
        {
            try
            {
                if(message.bot_id == SocketClient.MySelf.id) { return; }
                if(!message.IsDirect()) { return; }
                if( message.IsByBot()) { return; }

                message.username = UserCache.SingleOrDefault(u => u.id == message.user).name ?? "[BLANK]";
                _logger.Information("[Message found from '{FromUser}/{FromUserName}']", message.user, message.username);
                _logger.Debug($"MSG: {message.text.SafeSubstring(0, 90)}");

                bool messageUnderstood = false;
                foreach (var resource in _messageHandlers)
                {
                    if (resource.HandleMessage(message).Result)
                    {
                        messageUnderstood = true;
                    }
                }
                if (messageUnderstood == false)
                {
                    SendMessage(message.CreateResponseMessage(
                        $"I'm sorry, I didn't catch that. If you have any further questions please direct them to #{_botConfig.BotRoom}."))
                        .Wait(5000);
                }
            }
            catch (Exception e)
            {
                _logger.Error(e, "Failure handling incoming message from '{FromUser}/{FromUserName}'. [MSG: {UserMessage}]", message.user, message.username, message.text.SafeSubstring(0, 90));
                Environment.FailFast($"Failure handling incoming message from '{message.user}/{message.username}'. [MSG: {message.text.SafeSubstring(0, 90)}]", e);
            }
        }
        private void OnDisconnect()
        {
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

        private void OnSocketInitialized()
        {
            _logger.Information("Socket initialized.");
        }
     

        private Task Disconnect()
        {
            _isDisconnecting = true;

            if (SocketClient != null && SocketClient.IsConnected)
            {
                SocketClient.CloseSocket();
            }
            return Task.CompletedTask;
        }
      

        internal void Reconnect()
        {
            _logger.Information("Reconnecting...");
            if (SocketClient != null)
            {
                SocketClient.OnMessageReceived -= OnMessageReceived;
                SocketClient.OnConnectionLost -= OnDisconnect;
                SocketClient.OnHello -= OnSocketInitialized;
            }

            _isDisconnecting = false;
            ConnectSocket()
                .ContinueWith(task =>
                {
                    if (task.IsCompleted && !task.IsCanceled && !task.IsFaulted)
                    {
                        _logger.Information("SlackConnection restored.");
                    }
                    else
                    {
                        _logger.Information($"Error while reconnecting: {task.Exception}");
                    }
                })
                .Wait();
        }

      


        public async Task SendMessage(MessageToSend message)
        {
            if (_isDisconnecting)
            {
                _logger.Warning("Trying to send message while disconnecting.");
            }
            var channelId = await GetChannelId(message);
            var res = await Client.PostMessageAsync(channelId, message.Text, "Automa Luce Della Pizza (PizzaLight Bot)");
        }

        private async Task<string> GetChannelId(MessageToSend message)
        {
            if (!string.IsNullOrEmpty(message.ChannelId)) return message.ChannelId;
            if (!string.IsNullOrEmpty(message.ChannelName))
            {
                //could open channel if not available, but thats outside the scope of this bot for now, so we'll let it fail
                var channel = Client.Channels.Single(c => c.name == message.ChannelName);
                if (channel != null) return channel.id;
            };
            var dmchannel = Client.DirectMessages.SingleOrDefault(dm => dm.user == message.UserId);
            if (dmchannel != null) return dmchannel.id;

            var res = await OpenUserConversation(message.UserId);
            //skulle gjerne ha oppdatert  dm-cache her, men har ikke enkel tilgang på objektet
            return res.id;
        }

        private async Task<Conversation> GetConversation(MessageToSend message)
        {
            if (!string.IsNullOrEmpty(message.ChannelId) && Client.DirectMessageLookup.ContainsKey(message.ChannelId))
            {
                return Client.DirectMessageLookup[message.ChannelId];
            }
            if(Client.DirectMessages.Any(dm=>dm.user == message.UserId))
            {
                return Client.DirectMessages.Single(dm => dm.user == message.UserId);
            }
            return await OpenUserConversation(message.UserId);
            //skulle gjerne ha oppdatert  dm-cache her, men har ikke enkel tilgang på objektet
        }

        public async Task<Conversation> OpenUserConversation(string userId)
        {
            var res=  await Client.APIRequestWithTokenAsync<JoinDirectMessageChannelResponse>(
                new Tuple<string, string>("users", userId),
                new Tuple<string, string>("return_im", "true")
                );
            res.AssertOk();
            return res.channel;
        }


    }

    public interface IPizzaCore
    {
        List<User> UserCache { get; }
        List<Channel> Channels { get; }
        bool IsConnected { get; }

        Task SendMessage(MessageToSend responseMessage);
        Task Start();
        Task Stop();
        void AddMessageHandlerToPipeline(params IMessageHandler[] messageHandlers);
    }

}