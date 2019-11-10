using Noobot.Core.MessagingPipeline.Request;
using PizzaLight.Infrastructure;
using PizzaLight.Models;
using PizzaLight.Resources.ExtensionClasses;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PizzaLight.Resources
{
    public class OptOutHandler : IOptOutHandler
    {
        private ILogger _logger;
        private BotConfig _config;
        private IOptOutState _state;
        private IPizzaCore _core;
        private IActivityLog _activityLog;
        private Func<DateTimeOffset> _funcNow;
        private Dictionary<string, OptOutOption> _pendingConfirmations = new Dictionary<string, OptOutOption>();
        
        const string OptOutString = "opt out";
        const string OptString = "opt";
        const string OptInString = "opt in";

        public OptOutHandler(ILogger logger, BotConfig config, IOptOutState state, IPizzaCore core, IActivityLog activityLog, Func<DateTimeOffset> funcNow)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _core = core ?? throw new ArgumentNullException(nameof(core));
            _activityLog = activityLog ?? throw new ArgumentNullException(nameof(activityLog));
            _funcNow = funcNow ?? throw new ArgumentNullException(nameof(funcNow));
        }

        public async Task<bool> HandleMessage(IncomingMessage incomingMessage)
        {
            var text = incomingMessage.FullText.ToLowerInvariant().TrimEnd();
            if (!text.StartsWith(OptString)) { return false; }

            if (text.StartsWith(OptOutString)){
                if(OptOutString.Equals(text))
                {
                    var message = incomingMessage.ProvideChannelOptionToOptOut(_config);
                    await _core.SendMessage(message);
                }
                else
                {
                    var channel = text.Substring(OptOutString.Length).Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                    await TryOptOutChannel(incomingMessage, channel);
                }

                return true;
            }
            else if (text.StartsWith(OptInString))
            {
                if (OptInString.Equals(text))
                {
                    //requires paramter channel
                }
                else
                {
                    var channel = text.Substring(OptInString.Length).Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                    await TryOptInChannel(incomingMessage, channel);
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        private async Task TryOptOutChannel(IncomingMessage incomingMessage, string channel)
        {
            if(channel[0] == '#')
            {
                channel = channel.Substring(1, channel.Length - 1);
            }

            if ("all".Equals(channel))
            {
                await UserOptsOutOfChannel(incomingMessage, _config.PizzaRoom.Room);
            }
            else if (_config.PizzaRoom.Room == channel )
            {
                await UserOptsOutOfChannel(incomingMessage, channel);
            }
            else
            {
                await _core.SendMessage(incomingMessage.ChannelUnrecogised(channel));
            }
        }

        private async Task UserOptsOutOfChannel(IncomingMessage incoming, string channel)
        {
            if (_pendingConfirmations.ContainsKey(incoming.UserId))
            {
                var _pending = _pendingConfirmations[incoming.UserId];
                _pendingConfirmations.Remove(incoming.UserId);

                if (_funcNow() < _pending.OptionTime.AddMinutes(2))
                {
                    _activityLog.Log($"User {incoming.Username} has opted out of pizza plans in channel {channel}.");
                    await _state.AddUserToOptOutOfChannel(incoming.GetSendingUser(), channel);
                    await _core.SendMessage(incoming.ConfirmOptOutMessage(channel));
                }
                else
                {
                    _pendingConfirmations.Add(incoming.UserId, new OptOutOption(incoming.GetSendingUser(), channel, _funcNow()));
                    var message = incoming.RepeatOptOutMessageToConfirm(channel);
                    await _core.SendMessage(message);
                }
            }
            else
            {
                _pendingConfirmations.Add(incoming.UserId, new OptOutOption(incoming.GetSendingUser(), channel, _funcNow()));
                var message = incoming.RepeatOptOutMessageToConfirm(channel);
                await _core.SendMessage(message);
            }
        }

        private async Task TryOptInChannel(IncomingMessage incoming, string channel)
        {
            if (channel[0] == '#')
            {
                channel = channel.Substring(1, channel.Length - 1);
            }

            if ("all".Equals(channel))
            {
                await UserOptsIntoChannel(incoming, _config.PizzaRoom.Room);
            }
            else if (_config.PizzaRoom.Room == channel)
            {
                await UserOptsIntoChannel(incoming, channel);
            }
            else
            {
                await _core.SendMessage(incoming.ChannelUnrecogised(channel));
            }
        }

        private async Task UserOptsIntoChannel(IncomingMessage incoming, string channel)
        {
            if (channel[0] == '#')
            {
                channel = channel.Substring(1, channel.Length - 1);
            }

            if (_state.ChannelList.ContainsKey(channel))
            {
                _activityLog.Log($"User {incoming.Username} has opted in for pizza plans again in channel {channel}.");
                await _state.RemoveUserFromOptOutOfChannel(incoming.GetSendingUser(), channel);
                await _core.SendMessage(incoming.OptedIntoChannelAgain(channel));
            }
            else
            {
                await _core.SendMessage(incoming.ChannelUnrecogised(channel));
            }
        }

        public Task Start()
        {
            return _state.Start();        }

        public Task Stop()
        {
            return _state.Stop();
        }

        internal class OptOutOption
        {
            public OptOutOption(Person user, string channel, DateTimeOffset timestamp)
            {
                User = user;
                Channel = channel;
                OptionTime = timestamp;
            }
            public Person User { get; set; }
            public string Channel { get; set; }
            public DateTimeOffset OptionTime { get; set; }
        }
    }

    public interface IOptOutHandler : IMessageHandler, IMustBeInitialized
    {
    }
}
