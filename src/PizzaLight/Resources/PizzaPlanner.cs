using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Noobot.Core.MessagingPipeline.Request;
using Noobot.Core.MessagingPipeline.Response;
using PizzaLight.Infrastructure;
using PizzaLight.Models;
using Serilog;

namespace PizzaLight.Resources
{
    public class PizzaPlanner : IMessageHandler, IMustBeInitialized
    {
        string ACTIVEEVENTSFILE = "active plans";
        private readonly ILogger _logger;
        private readonly JsonStorage _storage;
        private readonly PizzaInviter _pizzaInviter;
        private readonly BotConfig _config;

        private PizzaCore _core;
        private List<PizzaPlan> _acitveplans;

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
            _acitveplans = _storage.ReadFile<PizzaPlan>(ACTIVEEVENTSFILE).ToList();

            _pizzaInviter.OnInvitationChanged += InvitationChanged;
            OnPlanChanged += PlanChanged;

            //må starte en scheduler som sjekker jevnlig om den skal lage ny plan
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
            //trenger logikk for å lage nye event om ingen planlagt neste uke eler så
            if (!_acitveplans.Any())
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
            var newPlan = new PizzaPlan()
            {
                Id = eventId,
                TimeOfEvent = timeOfEvent,
                Invited = toInvite.ToList(),
            };

            _acitveplans.Add(newPlan);
            _storage.SaveFile(ACTIVEEVENTSFILE, _acitveplans.ToArray());
            var inviteList = toInvite.Select(i => new Invitation()
            {
                EventId = eventId,
                UserId = i.UserId,
                UserName = i.UserName,
                EventTime = timeOfEvent
            });
            _pizzaInviter.AddToInviteList(inviteList);
        }

        private List<Person> GetPeopleToInvite()
        {
            var roomName = _config.Channels.First();
            var channel = _core.SlackConnection.ConnectedHubs.Values.SingleOrDefault(r => r.Name == $"#{roomName}");
            if (channel == null)
            {
                _logger.Warning("No such room: ", roomName);
                return new List<Person>();
            }

            var inviteCandidates = _core.SlackConnection.UserCache.Values.Where(u => channel.Members.Contains(u.Id)).Where(m => !m.IsBot).ToList();
            var random = new Random();

            var numberOfCandidates = inviteCandidates.Count;
            var numberToInvite = _config.InvitesPerEvent;
            numberToInvite = Math.Min(numberToInvite, numberOfCandidates);
            var inviteList = new List<Person>(numberToInvite);
            for (int i = 0; i < numberToInvite; i++)
            {
                var randomPick = random.Next(0, numberOfCandidates);
                var invite = inviteCandidates[randomPick];
                inviteCandidates.Remove(invite);
                numberOfCandidates--;
                inviteList.Add(new Person(){UserName=invite.Name, UserId = invite.Id});
            }
            return inviteList;
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

        private async Task InvitationChanged(Invitation invitation)
        {
            try
            {
                var pizzaEvent = _acitveplans.Single(e => e.Id == invitation.EventId);
                var person = pizzaEvent.Invited
                    .SingleOrDefault(i => i.UserName == invitation.UserName && invitation.Response== Invitation.ResponseEnum.NoResponse);
                if (person == null)
                {   return;}


                pizzaEvent.Invited.Remove(person);
                if (invitation.Response == Invitation.ResponseEnum.Accepted)
                {
                    pizzaEvent.Accepted.Add(person);
                }
                if (invitation.Response == Invitation.ResponseEnum.Rejected)
                {
                    pizzaEvent.Rejected.Add(person);
                }

                _storage.SaveFile(ACTIVEEVENTSFILE, _acitveplans.ToArray());
                await RaiseOnOnPlanChanged(pizzaEvent);
            }
            catch (Exception e)
            {
                _logger.Error(e, "Unable to apply event InvitationChanged");
                throw;
            }
        }

        private async Task PlanChanged(PizzaPlan pizzaPlan)
        {
            if (pizzaPlan.Accepted.Any() && !pizzaPlan.Invited.Any())
            {
                await LockParticipants(pizzaPlan.Id);
            }
        }

        private async Task LockParticipants(string id)
        {
            var pizzaEvent = _acitveplans.Single(e => e.Id == id);
            pizzaEvent.ParticipatntsLocked = true;
            var day = pizzaEvent.TimeOfEvent.LocalDateTime.ToString("dddd, MMMM dd");
            var time = pizzaEvent.TimeOfEvent.LocalDateTime.ToString("HH:mm");
            var participantlist = string.Join(",", pizzaEvent.Accepted.Select(a=>$"@{a.UserName}"));
            var text = $"Great news! This amazing group of friends have accepted the invitation for a pizza date on *{day} at {time}* \n" +
                       $"{participantlist} \n" +
                       $"If you don't know them all yet, now is an excellent opportunity. Please have a fantatic time!";

            foreach (var person in pizzaEvent.Accepted)
            {
                var message = new ResponseMessage()
                {
                    ResponseType = ResponseType.DirectMessage,
                    UserId = person.UserId,
                    Text = text,
                };
                await _core.SendMessage(message);
            }
            _storage.SaveFile(ACTIVEEVENTSFILE, _acitveplans.ToArray());
        }


        public delegate Task PlanChangedEventHandler(PizzaPlan pizzaPlan);
        public event PlanChangedEventHandler OnPlanChanged;
        private async Task RaiseOnOnPlanChanged(PizzaPlan pizzaPlan)
        {
            var ev = OnPlanChanged;
            if (ev != null)
            {
                try
                {
                    await ev(pizzaPlan);
                }
                catch (Exception ex
                )
                {
                    _logger.Error(ex, "Error Raising InvitationChanged.");
                }
            }
        }
    }
}