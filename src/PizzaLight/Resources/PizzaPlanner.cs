using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Noobot.Core.MessagingPipeline.Request;
using Noobot.Core.MessagingPipeline.Response;
using PizzaLight.Infrastructure;
using PizzaLight.Models;
using PizzaLight.Resources.ExtensionClasses;
using SlackConnector.Models;
using ILogger = Serilog.ILogger;

namespace PizzaLight.Resources
{
    public class PizzaPlanner : IMustBeInitialized
    {
        string ACTIVEEVENTSFILE = "active plans";
        private readonly IActivityLog _activityLog;
        private readonly ILogger _logger;
        private readonly IFileStorage _storage;
        private readonly IPizzaInviter _pizzaInviter;
        private readonly BotConfig _config;

        private IPizzaCore _core;
        private List<PizzaPlan> _acitveplans;
        private Timer _timer;

        public List<PizzaPlan> PizzaPlans => _acitveplans;

        public PizzaPlanner(ILogger logger, BotConfig config, IFileStorage storage, IPizzaInviter pizzaInviter, IPizzaCore core, IActivityLog activityLog)
        {
            _activityLog = activityLog;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _pizzaInviter = pizzaInviter ?? throw new ArgumentNullException(nameof(pizzaInviter));
            _core = core ?? throw new ArgumentNullException(nameof(core));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public async Task Start()
        {
            _logger.Debug("Starting Pizza Planner.");
            await _storage.Start();
            _acitveplans = _storage.ReadFile<PizzaPlan>(ACTIVEEVENTSFILE).ToList();

            _pizzaInviter.OnInvitationChanged += HandleInvitationChanged;
            OnPlanChanged += HandlePlanChanged;

            _timer = new Timer(async state => await FollowUpOnActivePizzaPlans(state), null, TimeSpan.FromSeconds(10), TimeSpan.FromMinutes(10));

            //må starte en scheduler som sjekker jevnlig om den skal lage ny plan
            await ScheduleNewEvents();
        }

        private async Task FollowUpOnActivePizzaPlans(object state)
        {
            try
            {
                await NominatePersonToMakeReservation();
                await NominatePersonToHandleExpenses();
            }
            catch (Exception e)
            {
                _logger.Fatal(e, "Exception running 'FollowUpOnActivePizzaPlans'");
            }
        }


        public async Task Stop()
        {
            await _storage.Stop();
            _logger.Information($"{this.GetType().Name} stopped succesfully.");
        }

        private async Task ScheduleNewEvents()
        {
            //trenger logikk for å lage nye event om ingen planlagt neste uke eler så
            if (!_acitveplans.Any())
            {
                await ScheduleNewEvent();
            }
        }

#pragma warning disable 1998
        private async Task ScheduleNewEvent()
#pragma warning restore 1998
        {
            _logger.Debug("Creating new event...");
            
            var eventId = Guid.NewGuid().ToString();
            var timeOfEvent = GetTimeOfNextEvent();
            var toInvite = FindPeopleToInvite(_config.RoomToInviteFrom, _config.InvitesPerEvent, new List<Person>());

            var newPlan = new PizzaPlan()
            {
                Id = eventId,
                TimeOfEvent = timeOfEvent,
                Invited = toInvite.ToList(),
                Channel = _config.RoomToInviteFrom
            };
            var inviteList = toInvite.Select(i => new Invitation()
            {
                EventId = eventId,
                UserId = i.UserId,
                UserName = i.UserName,
                EventTime = timeOfEvent
            }).ToList();
            _pizzaInviter.Invite(inviteList);

            _activityLog.Log($"Created a new Pizza Plan for {inviteList.Count} participants.");
            _acitveplans.Add(newPlan);
            _storage.SaveFile(ACTIVEEVENTSFILE, _acitveplans.ToArray());
        }

        public List<Person> FindPeopleToInvite(string channelName, int targetGuestCount, IEnumerable<Person> ignoreUsers)
        {
            var channel = _core.SlackConnection.ConnectedHubs.Values.SingleOrDefault(r => r.Name == $"#{channelName}");
            if (channel == null)
            {
                _logger.Warning("No such room: ", channelName);
                return new List<Person>();
            }
            var channelMembers = _core.UserCache.Values.Where(u => channel.Members.Contains(u.Id)).Where(m => !m.IsBot);
            var inviteCandidates = channelMembers.Where(c => ignoreUsers.All(u => u.UserName != c.Name)).ToList();
            inviteCandidates = inviteCandidates.Where(c => c.Name == "jarlelin").ToList();

            return inviteCandidates.SelectListOfRandomPeople(targetGuestCount);
        }

        public DateTimeOffset GetTimeOfNextEvent()
        {
            //onsdag i uken etter neste - todo 
            var today = DateTimeOffset.UtcNow;
            var targetDay = today
                .AddDays(- (int) today.DayOfWeek + 3)
                .AddDays(14);

            //at 17:00 
            return targetDay.Date.AddHours(17);
        }

        private async Task HandleInvitationChanged(Invitation invitation)
        {
            try
            {
                var pizzaEvent = _acitveplans.Single(e => e.Id == invitation.EventId);
                var person = pizzaEvent.Invited
                    .SingleOrDefault(i => i.UserName == invitation.UserName && invitation.Response != Invitation.ResponseEnum.NoResponse);
                if (person == null)
                {   return;}

                if (pizzaEvent.Invited.Contains(person))
                {
                    pizzaEvent.Invited.Remove(person);
                }
                if (invitation.Response == Invitation.ResponseEnum.Accepted)
                {
                    pizzaEvent.Accepted.Add(person);
                }
                if (invitation.Response == Invitation.ResponseEnum.Rejected || invitation.Response == Invitation.ResponseEnum.Expired)
                {
                    pizzaEvent.Rejected.Add(person);
                }

                _storage.SaveFile(ACTIVEEVENTSFILE, _acitveplans.ToArray());
                await RaiseOnOnPlanChanged(pizzaEvent);
            }
            catch (Exception e)
            {
                _logger.Error(e, "Unable to apply event InvitationChanged");
            }
        }

        private async Task HandlePlanChanged(PizzaPlan pizzaPlan)
        {
            var totalInvited = pizzaPlan.Accepted.Count + pizzaPlan.Invited.Count;
            if (totalInvited < _config.InvitesPerEvent)
            {
                var ignoreUsers = pizzaPlan.Rejected.ToList();
                ignoreUsers.AddRange(pizzaPlan.Accepted);
                var newGuests = FindPeopleToInvite(_config.RoomToInviteFrom, _config.InvitesPerEvent-totalInvited, ignoreUsers);
                var inviteList = newGuests.Select(i => new Invitation()
                {
                    EventId = pizzaPlan.Id,
                    UserId = i.UserId,
                    UserName = i.UserName,
                    EventTime = pizzaPlan.TimeOfEvent
                }).ToList();
                if (!inviteList.Any())
                {
                    _logger.Information("Found no more eligible users to invite to event {EventId}.", pizzaPlan.Id);
                }
                else
                {
                    _activityLog.Log($"Added {inviteList.Count} more guests to event {pizzaPlan.Id}");
                }
                _pizzaInviter.Invite(inviteList);
                pizzaPlan.Invited.AddRange(newGuests);
                _storage.SaveFile(ACTIVEEVENTSFILE, _acitveplans.ToArray());
            }

            if (pizzaPlan.Accepted.Any() && !pizzaPlan.Invited.Any())
            {
                await LockParticipants(pizzaPlan);
            }
        }

        private async Task LockParticipants(PizzaPlan pizzaPlan)
        {
            var participantlist = string.Join(", ", pizzaPlan.Accepted.Select(a => $"@{a.UserName}"));
            _activityLog.Log($"Locking pizza plan {pizzaPlan.Id} with  participants ({pizzaPlan.Accepted.Count}) {participantlist}");

            pizzaPlan.ParticipantsLocked = true;

            foreach (var person in pizzaPlan.Accepted)
            {
                var message = pizzaPlan.CreateParticipantsLockedResponseMessage(participantlist, person);
                await _core.SendMessage(message);
            }
            _storage.SaveFile(ACTIVEEVENTSFILE, _acitveplans.ToArray());
        }

        private async Task NominatePersonToMakeReservation()
        {
            PizzaPlan pizzaPlan;
            while((pizzaPlan = _acitveplans.FirstOrDefault(p=>p.ParticipantsLocked && p.PersonDesignatedToMakeReservation == null)) != null)
            {
                var candidates = _core.UserCache.Where(c => pizzaPlan.Accepted.Any(a => a.UserId == c.Key)).Select(c => c.Value).ToList();
                candidates = candidates.Where(c=>c.Name != pizzaPlan.PersonDesignatedToHandleExpenses?.UserName).ToList();
                var guest = candidates.SelectListOfRandomPeople(1).SingleOrDefault();
                if (guest == null)
                {
                    _logger.Warning("No eligible candidates for making reservation.");
                    return;
                }

                pizzaPlan.PersonDesignatedToMakeReservation = guest;
                _storage.SaveFile(ACTIVEEVENTSFILE, _acitveplans.ToArray());
                _activityLog.Log($"Chose {pizzaPlan?.PersonDesignatedToMakeReservation?.UserName} to make reservations for pizzaplan {pizzaPlan.Id}");

                var message = pizzaPlan.CreateNewDesignateToMakeReservationMessage();
                await _core.SendMessage(message);
            }
        }
        private async Task NominatePersonToHandleExpenses()
        {
            PizzaPlan pizzaPlan;
            while ((pizzaPlan = _acitveplans.FirstOrDefault(p => p.ParticipantsLocked && p.PersonDesignatedToHandleExpenses == null)) != null)
            {
                var candidates = _core.UserCache.Where(c => pizzaPlan.Accepted.Any(a => a.UserId == c.Key)).Select(c => c.Value).ToList();
                candidates = candidates.Where(c => c.Name != pizzaPlan.PersonDesignatedToMakeReservation?.UserName).ToList();
                var guest = candidates.SelectListOfRandomPeople(1).SingleOrDefault();
                if (guest == null)
                {
                    _logger.Warning("No eligible candidates for handling expenses.");
                    return;
                }

                pizzaPlan.PersonDesignatedToHandleExpenses = guest;
                _storage.SaveFile(ACTIVEEVENTSFILE, _acitveplans.ToArray());
                _activityLog.Log($"Chose {pizzaPlan?.PersonDesignatedToHandleExpenses?.UserName} to handle expenses for pizzaplan {pizzaPlan.Id}");

                var message = pizzaPlan.CreateNewDesignateToHandleExpensesMessage();
                await _core.SendMessage(message);
            }
        }


        public delegate Task PlanChangedEventHandler(PizzaPlan pizzaPlan);
        public event PlanChangedEventHandler OnPlanChanged;
        private async Task RaiseOnOnPlanChanged(PizzaPlan pizzaPlan)
        {
            var ev = OnPlanChanged;
            if (ev != null)
            {
                 await ev(pizzaPlan);
            }
        }

    }
}