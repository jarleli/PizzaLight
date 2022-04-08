using Bip39Words;
using PizzaLight.Infrastructure;
using PizzaLight.Models;
using PizzaLight.Resources.ExtensionClasses;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ILogger = Serilog.ILogger;

namespace PizzaLight.Resources
{
    public class PizzaPlanner : IMustBeInitialized
    {
        // ReSharper disable InconsistentNaming
        public const string ACTIVEEVENTSFILE = "activeplans";
        public const string OLDEVENTSFILE = "oldplans";
        public const int DAYSBEFOREEVENTTOCANCEL = 5;
        public const int HOURSBEFORETOREMIND = 47;
        private const int MINIMUMPARTICIPANTS = 4;

        // ReSharper restore InconsistentNaming

        private readonly IActivityLog _activityLog;
        private readonly Func<DateTimeOffset> _funcNow;
        private readonly ILogger _logger;
        private readonly IFileStorage _storage;
        private readonly IPizzaInviter _pizzaInviter;
        private readonly BotConfig _config;

        private IPizzaCore _core;
        private readonly IOptOutState _optOutState;
        private List<PizzaPlan> _activePlans;
        private Timer _timer;

        public List<PizzaPlan> PizzaPlans => _activePlans;

        public PizzaPlanner(ILogger logger, BotConfig config, IFileStorage storage, IPizzaInviter pizzaInviter, IPizzaCore core, IOptOutState optOutState, IActivityLog activityLog, Func<DateTimeOffset> funcNow)
        {
            _activityLog = activityLog ?? throw new ArgumentNullException(nameof(activityLog));
            _funcNow = funcNow ?? throw new ArgumentNullException(nameof(funcNow));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _pizzaInviter = pizzaInviter ?? throw new ArgumentNullException(nameof(pizzaInviter));
            _core = core ?? throw new ArgumentNullException(nameof(core));
            _optOutState = optOutState ?? throw new ArgumentNullException(nameof(optOutState));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public async Task Start()
        {
            _logger.Debug("Starting Pizza Planner.");

            if (string.IsNullOrEmpty(_config.PizzaRoom.City))
                throw new ConfigurationErrorsException($"Config element cannot be null 'PizzaRoom.City'"); 

            if (string.IsNullOrEmpty(_config.PizzaRoom.Room))
                throw new ConfigurationErrorsException($"Config element cannot be null 'PizzaRoom.Room'");
        
            if(string.IsNullOrEmpty(_config.BotRoom))
                throw new ConfigurationErrorsException($"Config element cannot be null 'BotRoom'");

            var channelName = $"#{_config.PizzaRoom.Room}";
            if (_core.Channels.Any(c=>c.name == channelName))
            {
                //var newHub = await _core.SlackConnection.JoinChannel(_config.PizzaRoom.Room);
                var message = $"Bot not in channel {_config.PizzaRoom.Room} and bots cannot join rooms on their own. Invite it!";
                _activityLog.Log(message);
                throw new InvalidOperationException(message);
            }

            await _storage.Start();
            _activePlans = _storage.ReadArray<PizzaPlan>(ACTIVEEVENTSFILE).ToList();

            _pizzaInviter.OnInvitationChanged += HandleInvitationChanged;

            _timer = new Timer(async _ => await PizzaPlannerLoopTick(), null, TimeSpan.FromSeconds(10), TimeSpan.FromMinutes(10));
        }

        public async Task PizzaPlannerLoopTick()
        {
            try
            {
                await ClosePizzaPlanAfterFinished();
                await LockInPizzaPlansOrCancelOnesThatPassDeadline();
                await NominatePersonToMakeReservation();
                await NominatePersonToHandleExpenses();
                await RemindParticipantsOfEvent();
                await ScheduleNewEventsIfThereIsNoPlannedEventTwoWeeksFromNow();
                await AnnouncePizzaPlanInPizzaRoom();
                await HandlePlansWithMissingInvitations();
            }
            catch (Exception e)
            {
                _activityLog.Log($"ERROR: {e.Message}");
                _logger.Fatal(e, "Exception running 'PizzaPlannerLoopTick'");
                Environment.Exit(-1);
            }
        }

        public async Task Stop()
        {
            await _storage.Stop();
            _logger.Information($"{this.GetType().Name} stopped succesfully.");
        }
        public async Task LockInPizzaPlansOrCancelOnesThatPassDeadline()
        {
            PizzaPlan pizzaPlan;
            while ((pizzaPlan =_activePlans.SingleOrDefault(p => !p.ParticipantsLocked && p.Accepted.Count >= _config.InvitesPerEvent)) != null)
            {
                await LockParticipants(pizzaPlan);
            }

            var plansOverDeadline = _activePlans.Where(p => _funcNow().AddDays(DAYSBEFOREEVENTTOCANCEL) > p.TimeOfEvent).ToList();
            while ((pizzaPlan = plansOverDeadline.FirstOrDefault(p => !p.ParticipantsLocked && !p.Cancelled.HasValue)) != null)
            {
                if (pizzaPlan.Accepted.Count < MINIMUMPARTICIPANTS)
                {
                    await CancelPizzaPlan(pizzaPlan);
                }
                else
                {
                    await LockParticipants(pizzaPlan);
                }
            }
        }

        private async Task ScheduleNewEventsIfThereIsNoPlannedEventTwoWeeksFromNow()
        {
            var today = _funcNow().Date;
            var thisWeeksMonday = today.AddDays( - (int)today.DayOfWeek + 1);
            thisWeeksMonday = today.DayOfWeek == DayOfWeek.Sunday ? thisWeeksMonday.AddDays(-7) : thisWeeksMonday;
            var mondayInWeekAfterNext = thisWeeksMonday.AddDays(14);
            
            if (!_activePlans.Any(p => p.TimeOfEvent > mondayInWeekAfterNext && p.TimeOfEvent < mondayInWeekAfterNext.AddDays(7)))
            {
                await ScheduleNewEvent(mondayInWeekAfterNext);
            }
        }

        private async Task ScheduleNewEvent( DateTime mondayInWeekToScheduleEvent)
        {
            _logger.Debug("Creating new event...");
            
            var eventId = WordGenerator.GetRandomWordString(3);
            var timeOfEvent = GetTimeOfNextEvent(mondayInWeekToScheduleEvent);
            var toInvite = await FindPeopleToInvite(_config.PizzaRoom.Room, _config.InvitesPerEvent, new List<Person>());

            var newPlan = new PizzaPlan()
            {
                Id = eventId,
                TimeOfEvent = timeOfEvent,
                Invited = toInvite.ToList(),
                Channel = _config.PizzaRoom.Room,
                City = _config.PizzaRoom.City
            };
            var inviteList = toInvite.Select(i => new Invitation()
            {
                EventId = eventId,
                UserId = i.UserId,
                UserName = i.UserName,
                EventTime = timeOfEvent,
                Room = newPlan.Channel,
                City = newPlan.City
            }).ToList();
            _pizzaInviter.Invite(inviteList);

            _activityLog.Log($"Created a new Pizza Plan '{newPlan.Id}' for {inviteList.Count} participants.");
            _activePlans.Add(newPlan);
            _storage.SaveArray(ACTIVEEVENTSFILE, _activePlans.ToArray());
        }

        public async Task<List<Person>> FindPeopleToInvite(string pizzaRoom, int targetGuestCount, IEnumerable<Person> peopleAlreadyInvitedToPizzaPlan)
        {
            var ignoredUsers = peopleAlreadyInvitedToPizzaPlan.ToList();
            var peopleWithOutstandingInvitations = _pizzaInviter.OutstandingInvites.Select(i => new Person() { UserName = i.UserName, UserId = i.UserId });
            ignoredUsers.AddRange(peopleWithOutstandingInvitations);
            var channelList = _optOutState.GetStateForChannel(pizzaRoom) ?? new ChannelState();
            var peopleThatHaveOptedOutOfReceivingInvitations = channelList.UsersThatHaveOptedOut;
            ignoredUsers.AddRange(peopleThatHaveOptedOutOfReceivingInvitations);


            var channel = _core.Channels.SingleOrDefault(c => c.name == $"{pizzaRoom}");
            if (channel == null)
            {
                throw new Exception($"No such room to invite from: '{pizzaRoom}'");
            }
            var channelMembers = await _core.ChannelMembers(pizzaRoom);
            var candidates = _core.UserCache
                .Where(u=> channelMembers.Contains(u.id))
                .Where(u=> ! u.is_bot)
                .Where(u=> ! u.deleted);
            var inviteCandidates = candidates.Where(cmm => ignoredUsers.All(u => u.UserId != cmm.id)).ToList();

            return inviteCandidates.SelectListOfRandomPeople(targetGuestCount);
        }

        public DateTimeOffset GetTimeOfNextEvent(DateTime dateInWeekToScheduleEvent)
        {
            //Day of week for event
            var dayOfWeekToHoldEvent = Random.Shared.Next(2, 5); //tuesday through thursday

            var targetDay = dateInWeekToScheduleEvent
                .AddDays(-(int) dateInWeekToScheduleEvent.DayOfWeek)
                .AddDays(dayOfWeekToHoldEvent);

            //at 17:00 
            return targetDay.Date.AddHours(17);
        }

        private Task HandleInvitationChanged(Invitation invitation)
        {
            try
            {
                var pizzaEvent = _activePlans.SingleOrDefault(e => e.Id == invitation.EventId);
                if (pizzaEvent == null)
                {
                    throw new InvalidOperationException($"No active event with id {invitation.EventId}");
                }
                var person = pizzaEvent.Invited
                    .SingleOrDefault(i => i.UserName == invitation.UserName && invitation.Response != Invitation.ResponseEnum.NoResponse);
                if (person == null)
                {
                    return Task.CompletedTask;
                }

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

                _storage.SaveArray(ACTIVEEVENTSFILE, _activePlans.ToArray());
            }
            catch (Exception e)
            {
                _logger.Error(e, "Unable to apply event InvitationChanged");
            }
            return Task.CompletedTask;
        }


#pragma warning disable 1998
        private async Task AddNewInvitationsToPlan(PizzaPlan pizzaPlan)
#pragma warning restore 1998
        {
            var totalInvited = pizzaPlan.Accepted.Count + pizzaPlan.Invited.Count;
            if (totalInvited < _config.InvitesPerEvent)
            {
                var ignoreUsers = pizzaPlan.Rejected.ToList();
                ignoreUsers.AddRange(pizzaPlan.Accepted);
                var newInvites = await FindPeopleToInvite(_config.PizzaRoom.Room, _config.InvitesPerEvent-totalInvited, ignoreUsers);
                if (!newInvites.Any())
                {
                    _logger.Information("Found no more eligible users to invite to event '{EventId}'.", pizzaPlan.Id);
                }
                else
                {
                    _logger.Debug("Adding {InvitedCount} more guests to event '{PizzaPlanId}'", newInvites.Count, pizzaPlan.Id);
                }

                var inviteList = newInvites.Select(i => new Invitation()
                {
                    EventId = pizzaPlan.Id,
                    UserId = i.UserId,
                    UserName = i.UserName,
                    EventTime = pizzaPlan.TimeOfEvent,
                    Room = pizzaPlan.Channel,
                    City = pizzaPlan.City
                }).ToList();

                _pizzaInviter.Invite(inviteList);
                pizzaPlan.Invited.AddRange(newInvites);
                _storage.SaveArray(ACTIVEEVENTSFILE, _activePlans.ToArray());
            }
        }

        private async Task HandlePlansWithMissingInvitations()
        {
            foreach (PizzaPlan plan in _activePlans.Where(p => p.Accepted.Count + p.Invited.Count < 5))
            {
                await AddNewInvitationsToPlan(plan);

            }
        }

        private async Task NominatePersonToMakeReservation()
        {
            PizzaPlan pizzaPlan;
            while((pizzaPlan = _activePlans.FirstOrDefault(p=>p.ParticipantsLocked && p.PersonDesignatedToMakeReservation == null)) != null)
            {
                var candidates = _core.UserCache.Where(c => pizzaPlan.Accepted.Any(a => a.UserId == c.id)).ToList();
                candidates = candidates.Where(c=>c.name != pizzaPlan.PersonDesignatedToHandleExpenses?.UserName).ToList();
                var guest = candidates.SelectListOfRandomPeople(1).SingleOrDefault();
                if (guest == null)
                {
                    _logger.Warning($"No eligible candidates for making reservation '{pizzaPlan.Id}'.");
                    return;
                }

                pizzaPlan.PersonDesignatedToMakeReservation = guest;
                _storage.SaveArray(ACTIVEEVENTSFILE, _activePlans.ToArray());
                _activityLog.Log($"Chose {pizzaPlan?.PersonDesignatedToMakeReservation?.UserName} to make reservations for pizzaplan '{pizzaPlan.Id}'");

                var message = pizzaPlan.CreateNewDesignateToMakeReservationMessage(_config.BotRoom);
                await _core.SendMessage(message);
            }
        }

        private async Task NominatePersonToHandleExpenses()
        {
            PizzaPlan pizzaPlan;
            while ((pizzaPlan = _activePlans.FirstOrDefault(p => p.ParticipantsLocked && p.PersonDesignatedToHandleExpenses == null)) != null)
            {
                var candidates = _core.UserCache.Where(c => pizzaPlan.Accepted.Any(a => a.UserId == c.id)).ToList();
                candidates = candidates.Where(c => c.name != pizzaPlan.PersonDesignatedToMakeReservation?.UserName).ToList();
                var guest = candidates.SelectListOfRandomPeople(1).SingleOrDefault();
                if (guest == null)
                {
                    _logger.Warning("No eligible candidates for handling expenses.");
                    return;
                }

                pizzaPlan.PersonDesignatedToHandleExpenses = guest;
                _storage.SaveArray(ACTIVEEVENTSFILE, _activePlans.ToArray());
                _activityLog.Log($"Chose {pizzaPlan?.PersonDesignatedToHandleExpenses?.UserName} to handle expenses for pizzaplan '{pizzaPlan.Id}'");

                var message = pizzaPlan.CreateNewDesignateToHandleExpensesMessage(_config.BotRoom);
                await _core.SendMessage(message);
            }
        }

        private async Task AnnouncePizzaPlanInPizzaRoom()
        {
            PizzaPlan pizzaPlan;
            while ((pizzaPlan = _activePlans
                       .FirstOrDefault(p => p.PersonDesignatedToHandleExpenses != null
                                            && p.PersonDesignatedToMakeReservation != null
                                            && p.PizzaPlanAnnouncedInPizzaRoom == false))
                   != null)
            {
                var message = pizzaPlan.CreatePizzaRoomAnnouncementMessage(_config.BotRoom);
                await _core.SendMessage(message);

                pizzaPlan.PizzaPlanAnnouncedInPizzaRoom = true;
                _storage.SaveArray(ACTIVEEVENTSFILE, _activePlans.ToArray());
            }
        }

        private async Task CancelPizzaPlan(PizzaPlan pizzaPlan)
        {
            var messages = pizzaPlan.CreateNewEventIsCancelledMessage();
            foreach (var message in messages)
            {
                await _core.SendMessage(message);
            }
            _activePlans.Remove(pizzaPlan);
            _storage.SaveArray(ACTIVEEVENTSFILE, _activePlans.ToArray());
            pizzaPlan.Cancelled = _funcNow();
            await AddPlanToFinished(pizzaPlan);
            _activityLog.Log($"Cancelling '{pizzaPlan.Id}' because only {pizzaPlan.Accepted.Count} had accepted");
        }

        private async Task LockParticipants(PizzaPlan pizzaPlan)
        {
            pizzaPlan.ParticipantsLocked = true;

            var stringList = pizzaPlan.Accepted.GetStringListOfPeople();
            _activityLog.Log($"Locking pizza plan '{pizzaPlan.Id}' with  participants ({pizzaPlan.Accepted.Count}) {stringList}");

            var messages = pizzaPlan.CreateParticipantsLockedResponseMessage();
            foreach (var m in messages)
            {
                await _core.SendMessage(m);
            }
            _storage.SaveArray(ACTIVEEVENTSFILE, _activePlans.ToArray());
        }

        private async Task RemindParticipantsOfEvent()
        {
            PizzaPlan pizzaPlan;
            var toRemind = _activePlans.Where(p => _funcNow().AddHours(HOURSBEFORETOREMIND) > p.TimeOfEvent).ToList();
            while ((pizzaPlan = toRemind.FirstOrDefault(p=>p.SentReminder == null)) != null)
            {
                var messages = pizzaPlan.CreateRemindParticipantsOfEvent();
                foreach (var message in messages)
                {
                    await _core.SendMessage(message);
                }
                _activityLog.Log($"Sent reminders to guest of '{pizzaPlan.Id}' because it starts at {pizzaPlan.TimeOfEvent}");
                pizzaPlan.SentReminder = _funcNow();
                _storage.SaveArray(ACTIVEEVENTSFILE, _activePlans.ToArray());
            }
        }

        private async Task ClosePizzaPlanAfterFinished()
        {
            PizzaPlan pizzaPlan;
            var planFinished = _activePlans.Where(p => _funcNow() > p.TimeOfEvent ).ToList();
            while ((pizzaPlan = planFinished.FirstOrDefault(p=> p.FinishedSuccessfully == false)) != null)
            {
                _activePlans.Remove(pizzaPlan);
                _storage.SaveArray(ACTIVEEVENTSFILE, _activePlans.ToArray());
                pizzaPlan.FinishedSuccessfully = true;
                await AddPlanToFinished(pizzaPlan);
                _activityLog.Log($"Closing '{pizzaPlan.Id}' because it finished");
            }
        }

#pragma warning disable 1998
        public async Task AddPlanToFinished(PizzaPlan plan)
#pragma warning restore 1998
        {
            var oldPlans= _storage.ReadArray<PizzaPlan>(OLDEVENTSFILE).ToList();
            if (oldPlans.FirstOrDefault(p => p.Id == plan.Id) != null)
            {
                throw new InvalidOperationException($"Trying to add a finished pizza plan to old plans, but that pizza plan Id already exists {plan.Id}");
            }
            oldPlans.Add(plan);
            _storage.SaveArray(OLDEVENTSFILE, oldPlans.ToArray());;
        }
    }
}