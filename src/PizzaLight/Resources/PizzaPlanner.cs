using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Noobot.Core.MessagingPipeline.Request;
using PizzaLight.Infrastructure;
using Serilog;

namespace PizzaLight.Resources
{
    public class PizzaPlanner : IMessageHandler, IMustBeInitialized
    {
        string ACTIVEEVENTSFILE = "activeEvents";
        private readonly ILogger _logger;
        private readonly JsonStorage _storage;
        private readonly PizzaInviter _pizzaInviter;
        private readonly BotConfig _config;

        private PizzaCore _core;
        private List<PizzaEvent> _acitveEvents;

        public PizzaPlanner(ILogger logger, BotConfig config, JsonStorage storage, PizzaInviter pizzaInviter, PizzaCore core)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _pizzaInviter = pizzaInviter ?? throw new ArgumentNullException(nameof(pizzaInviter));
            _core = core ?? throw new ArgumentNullException(nameof(core));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public async Task Start()
        {
            _logger.Debug("Starting Pizza Planner");
            await _storage.Start();
            var activeEvents = _storage.ReadFile<PizzaEvent>(ACTIVEEVENTSFILE);
            _acitveEvents = activeEvents.ToList();
            await ScheduleNewEvents();
        }

        public async Task Stop()
        {
            await _storage.Stop();
            _logger.Information($"{this.GetType().Name} stopped succesfully.");
        }

        public async Task HandleMessage(IncomingMessage incomingMessage)
        {
        }

        private async Task ScheduleNewEvents()
        {
            if (!_acitveEvents.Any())
            {
                await ScheduleNewEvent();
            }
        }

        private async Task ScheduleNewEvent()
        {
            _logger.Information("Creating new event...");
            
            var eventId = Guid.NewGuid().ToString();
            var timeOfEvent = GetDayOfNextEvent().AddHours(17);
            var toInvite = GetPeopleToInvite();
            var newEvent = new PizzaEvent()
            {
                Id = eventId,
                TimeOfEvent = timeOfEvent,
                Invited = toInvite
            };

            _acitveEvents.Add(newEvent);
            _storage.SaveFile(ACTIVEEVENTSFILE, _acitveEvents.ToArray());
            var inviteList = toInvite.Select(i => new Invitation()
            {
                EventId = eventId,
                UserId = i.UserId,
                UserName = i.UserName,
                EventTime = timeOfEvent
            });
            _pizzaInviter.AddToInviteList(inviteList);
        }

        private Invite[] GetPeopleToInvite()
        {
            var roomName = _config.Channels.First();
            var channel = _core.SlackConnection.ConnectedHubs.Values.SingleOrDefault(r => r.Name == $"#{roomName}");
            if (channel == null)
            {
                _logger.Warning("No such room: ", roomName);
                return new Invite[0];
            }

            var memberslist = _core.SlackConnection.UserCache.Values.Where(u => channel.Members.Contains(u.Id));

            //kun jarle foreløpig 
            var usernameToInvite = "jarlelin";
            var user = memberslist.Single(m => m.Name == usernameToInvite);

            return new [] {new Invite() {UserName = user.Name, UserId = user.Id} };
        }

        private DateTimeOffset GetDayOfNextEvent()
        {
            //onsdag i uken etter neste
            var today = DateTimeOffset.UtcNow;
            var targetDay = today
                .AddDays(- (int) today.DayOfWeek + 3)
                .AddDays(14);
            return targetDay.Date;
        }

        public void SetCoreInstance(PizzaCore core)
        {
            if (_core != null)
            {
                throw new InvalidOperationException("Cant set core, already set.");
            }

            _core = core ?? throw new ArgumentNullException(nameof(core));
        }
    }
}