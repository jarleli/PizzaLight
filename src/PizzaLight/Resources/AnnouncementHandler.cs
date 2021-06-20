using Noobot.Core.MessagingPipeline.Response;
using PizzaLight.Infrastructure;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace PizzaLight.Resources
{
    public class AnnouncementHandler : IAnnouncementHandler
    {
        private const string STATEFILENAME = "state";
        private readonly IActivityLog _activity;
        private readonly IPizzaCore _core;
        private readonly IFileStorage _storage;
        private readonly BotConfig _config;

        public AnnouncementHandler(IActivityLog activity, IPizzaCore core, IFileStorage storage, BotConfig config)
        {
            _activity = activity ?? throw new ArgumentNullException(nameof(activity));
            _core = core ?? throw new ArgumentNullException(nameof(core));
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }
        public async Task Start()
        {
            await _storage.Start();
            var state = _storage.ReadObject<PizzaBotState>(STATEFILENAME) ?? new PizzaBotState();

            if (state.OptOutFeatureAnnounced == null)
            {
                _activity.Log("Tried to announce opt out feature, but announcement is disabled");
                return;

                var channels = new[] { _config.BotRoom, _config.PizzaRoom.Room };
                foreach (var channel in channels)
                {
                    var message = new ResponseMessage()
                    {
                        Channel = channel,
                        ResponseType = ResponseType.Channel,
                        Text = "Apparenly not everyone loves pizza as much as me. It appears not everyone here actually has the time or the ability to meet up for pizza with great friends.\n" +
                        "That is a great shame, but if that is you then I have just the thing. Starting now I have the ability to remember those that don't want to, or can't make it to our fabulous pizza dates. \n" +
                        "Try messaging me with the command `opt out` and I'll get you sorted and you won't have to be bothered by me again.\n" +
                        $"For everyone else: *more pizza for us!*\n" +
                        $"And if you have complaints or comments, I'm hanging out in #{_config.BotRoom}, so feel free to join me there."
                    };
                    await _core.SendMessage(message);
                    _activity.Log($"Announced feature:OptOut! in {channel}");
                }
                state.OptOutFeatureAnnounced = DateTimeOffset.UtcNow;

                _storage.SaveObject(STATEFILENAME, state);
            }

        }


        public async Task Stop()
        {
            await _storage.Stop();        
        }
    }
    public interface IAnnouncementHandler : IMustBeInitialized
    {
    }
}
