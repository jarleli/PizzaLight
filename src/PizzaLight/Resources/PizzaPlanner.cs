using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bip39Words;
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
        // ReSharper disable InconsistentNaming
        const string ACTIVEEVENTSFILE = "activeplans";
        const string OLDEVENTSFILE = "oldplans";
        const int DAYSBEFOREEVENTTOCANCEL = 5;
        const int HOURSBEFORETOREMIND = 47;
        // ReSharper restore InconsistentNaming

        private readonly IActivityLog _activityLog;
        private readonly ILogger _logger;
        private readonly IFileStorage _storage;
        private readonly IPizzaInviter _pizzaInviter;
        private readonly BotConfig _config;

        private IPizzaCore _core;
        private List<PizzaPlan> _activePlans;
        private Timer _timer;

        public List<PizzaPlan> PizzaPlans => _activePlans;

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
            _activePlans = _storage.ReadFile<PizzaPlan>(ACTIVEEVENTSFILE).ToList();

            _pizzaInviter.OnInvitationChanged += HandleInvitationChanged;
            OnPlanChanged += HandlePlanChanged;

            _timer = new Timer(async state => await PizzaPlannerScheduler(state), null, TimeSpan.FromSeconds(10), TimeSpan.FromMinutes(10));
        }

        private async Task PizzaPlannerScheduler(object state)
        {
            try
            {
                await CancelOrLockEventIfNotFullBeforeDeadline();
                await NominatePersonToMakeReservation();
                await NominatePersonToHandleExpenses();
                await RemindParticipantsOfEvent();
                await ClosePizzaPlanAfterFinished();
                await ScheduleNewEvents();
                await AnnouncePizzaPlanInPizzaRoom();
            }
            catch (Exception e)
            {
                _logger.Fatal(e, "Exception running 'PizzaPlannerScheduler'");
            }
        }

        

        public async Task Stop()
        {
            await _storage.Stop();
            _logger.Information($"{this.GetType().Name} stopped succesfully.");
        }

        private async Task ScheduleNewEvents()
        {
            var today = DateTimeOffset.UtcNow.Date;
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
            var peopleToNotInviteThisTime = _pizzaInviter.OutstandingInvites.Select(i=>new Person(){UserName = i.UserName, UserId = i.UserId});
            var toInvite = FindPeopleToInvite(_config.PizzaRoom.Room, _config.InvitesPerEvent, peopleToNotInviteThisTime);

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
            _storage.SaveFile(ACTIVEEVENTSFILE, _activePlans.ToArray());
        }

        public List<Person> FindPeopleToInvite(string channelName, int targetGuestCount, IEnumerable<Person> ignoreUsers)
        {
            var channel = _core.SlackConnection.ConnectedHubs.Values.SingleOrDefault(r => r.Name == $"#{channelName}");
            if (channel == null)
            {
                _logger.Warning("No such room: ", channelName);
                return new List<Person>();
            }
            var channelMembers = _core.UserCache.Values
                .Where(u=> channel.Members.Contains(u.Id))
                .Where(m=> ! m.IsBot)
                .Where(m=> ! m.Deleted);
            var inviteCandidates = channelMembers.Where(c => ignoreUsers.All(u => u.UserName != c.Name)).ToList();

            return inviteCandidates.SelectListOfRandomPeople(targetGuestCount);
        }

        public DateTimeOffset GetTimeOfNextEvent(DateTime dateInWeekToScheduleEvent)
        {
            //Day of week for event
            var random = new Random();
            var dayOfWeekToHoldEvent = random.Next(2, 5); //tuesday through thursday

            var targetDay = dateInWeekToScheduleEvent
                .AddDays(-(int) dateInWeekToScheduleEvent.DayOfWeek)
                .AddDays(dayOfWeekToHoldEvent);

            //at 17:00 
            return targetDay.Date.AddHours(17);
        }

        private async Task HandleInvitationChanged(Invitation invitation)
        {
            try
            {
                var pizzaEvent = _activePlans.Single(e => e.Id == invitation.EventId);
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

                _storage.SaveFile(ACTIVEEVENTSFILE, _activePlans.ToArray());
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
                ignoreUsers.AddRange(_pizzaInviter.OutstandingInvites.Select(i => new Person() { UserName = i.UserName, UserId = i.UserId }));
                var newGuests = FindPeopleToInvite(_config.PizzaRoom.Room, _config.InvitesPerEvent-totalInvited, ignoreUsers);
                var inviteList = newGuests.Select(i => new Invitation()
                {
                    EventId = pizzaPlan.Id,
                    UserId = i.UserId,
                    UserName = i.UserName,
                    EventTime = pizzaPlan.TimeOfEvent,
                    Room = pizzaPlan.Channel,
                    City = pizzaPlan.City
                }).ToList();
                if (!inviteList.Any())
                {
                    _logger.Information("Found no more eligible users to invite to event '{EventId}'.", pizzaPlan.Id);
                }
                else
                {
                    _activityLog.Log($"Added {inviteList.Count} more guests to event '{pizzaPlan.Id}'");
                }
                _pizzaInviter.Invite(inviteList);
                pizzaPlan.Invited.AddRange(newGuests);
                _storage.SaveFile(ACTIVEEVENTSFILE, _activePlans.ToArray());
            }

            if (pizzaPlan.Accepted.Any() && !pizzaPlan.Invited.Any())
            {
                await LockParticipants(pizzaPlan);
            }
        }

        private async Task LockParticipants(PizzaPlan pizzaPlan)
        {
            var stringList = pizzaPlan.Accepted.GetStringListOfPeople();
            _activityLog.Log($"Locking pizza plan '{pizzaPlan.Id}' with  participants ({pizzaPlan.Accepted.Count}) {stringList}");

            pizzaPlan.ParticipantsLocked = true;

            var messages = pizzaPlan.CreateParticipantsLockedResponseMessage();
            foreach (var m in messages)
            {
                await _core.SendMessage(m);
            }
            _storage.SaveFile(ACTIVEEVENTSFILE, _activePlans.ToArray());
        }

        private async Task NominatePersonToMakeReservation()
        {
            PizzaPlan pizzaPlan;
            while((pizzaPlan = _activePlans.FirstOrDefault(p=>p.ParticipantsLocked && p.PersonDesignatedToMakeReservation == null)) != null)
            {
                var candidates = _core.UserCache.Where(c => pizzaPlan.Accepted.Any(a => a.UserId == c.Key)).Select(c => c.Value).ToList();
                candidates = candidates.Where(c=>c.Name != pizzaPlan.PersonDesignatedToHandleExpenses?.UserName).ToList();
                var guest = candidates.SelectListOfRandomPeople(1).SingleOrDefault();
                if (guest == null)
                {
                    _logger.Warning($"No eligible candidates for making reservation '{pizzaPlan.Id}'.");
                    return;
                }

                pizzaPlan.PersonDesignatedToMakeReservation = guest;
                _storage.SaveFile(ACTIVEEVENTSFILE, _activePlans.ToArray());
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
                var candidates = _core.UserCache.Where(c => pizzaPlan.Accepted.Any(a => a.UserId == c.Key)).Select(c => c.Value).ToList();
                candidates = candidates.Where(c => c.Name != pizzaPlan.PersonDesignatedToMakeReservation?.UserName).ToList();
                var guest = candidates.SelectListOfRandomPeople(1).SingleOrDefault();
                if (guest == null)
                {
                    _logger.Warning("No eligible candidates for handling expenses.");
                    return;
                }

                pizzaPlan.PersonDesignatedToHandleExpenses = guest;
                _storage.SaveFile(ACTIVEEVENTSFILE, _activePlans.ToArray());
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
                _storage.SaveFile(ACTIVEEVENTSFILE, _activePlans.ToArray());
            }
        }

        private async Task CancelOrLockEventIfNotFullBeforeDeadline()
        {
            PizzaPlan pizzaPlan;
            var plansOverDeadline = _activePlans.Where(p => DateTimeOffset.UtcNow.AddDays(DAYSBEFOREEVENTTOCANCEL) > p.TimeOfEvent).ToList();
            while ((pizzaPlan = plansOverDeadline.FirstOrDefault(p => !p.ParticipantsLocked)) != null)
            {
                if (pizzaPlan.Accepted.Count < 4)
                {
                    var messages = pizzaPlan.CreateNewEventIsCancelledMessage();
                    foreach (var message in messages)
                    {
                        await _core.SendMessage(message);
                    }
                    _activePlans.Remove(pizzaPlan);
                    _storage.SaveFile(ACTIVEEVENTSFILE, _activePlans.ToArray());
                    pizzaPlan.Cancelled = DateTimeOffset.UtcNow;
                    await AddPlanToFinished(pizzaPlan);
                    _activityLog.Log(
                        $"Cancelling '{pizzaPlan.Id}' because only {pizzaPlan.Accepted.Count} had accepted");
                }
                else
                {
                    await LockParticipants(pizzaPlan);
                }
            }
        }

        private async Task RemindParticipantsOfEvent()
        {
            PizzaPlan pizzaPlan;
            var toRemind = _activePlans.Where(p => DateTimeOffset.UtcNow.AddHours(HOURSBEFORETOREMIND) > p.TimeOfEvent).ToList();
            while ((pizzaPlan = toRemind.FirstOrDefault(p=>p.SentReminder == null)) != null)
            {
                var messages = pizzaPlan.CreateRemindParticipantsOfEvent();
                foreach (var message in messages)
                {
                    await _core.SendMessage(message);
                }
                _activityLog.Log($"Sent reminders to guest of '{pizzaPlan.Id}' because it starts at {pizzaPlan.TimeOfEvent}");
                pizzaPlan.SentReminder = DateTimeOffset.UtcNow;
                _storage.SaveFile(ACTIVEEVENTSFILE, _activePlans.ToArray());
            }
        }


        private async Task ClosePizzaPlanAfterFinished()
        {
            PizzaPlan pizzaPlan;
            var planFinished = _activePlans.Where(p => DateTimeOffset.UtcNow > p.TimeOfEvent ).ToList();
            while ((pizzaPlan = planFinished.FirstOrDefault(p=> p.FinishedSuccessfully == false)) != null)
            {
                _activePlans.Remove(pizzaPlan);
                _storage.SaveFile(ACTIVEEVENTSFILE, _activePlans.ToArray());
                pizzaPlan.FinishedSuccessfully = true;
                await AddPlanToFinished(pizzaPlan);
                _activityLog.Log($"Closing '{pizzaPlan.Id}' because it finished");
            }
        }

        public async Task AddPlanToFinished(PizzaPlan plan)
        {
            var oldPlans= _storage.ReadFile<PizzaPlan>(OLDEVENTSFILE).ToList();
            oldPlans.Add(plan);
            _storage.SaveFile(OLDEVENTSFILE, oldPlans.ToArray());;
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