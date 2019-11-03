using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Noobot.Core.MessagingPipeline.Request;
using PizzaLight.Infrastructure;
using PizzaLight.Models;
using PizzaLight.Resources.ExtensionClasses;
using Serilog;
using SlackConnector.Models;

namespace PizzaLight.Resources
{
    
    public class PizzaInviter : IPizzaInviter
    {
        private readonly IActivityLog _activityLog;
        private readonly ILogger _logger;
        private readonly BotConfig _botConfig;
        private readonly IFileStorage _storage;
        private readonly IPizzaCore _core;
        private List<Invitation> _activeInvitations;
        private Timer _timer;

        // ReSharper disable InconsistentNaming
        public const string INVITESFILE = "activeinvites";
        private const int HOURSTOWAITBEFOREREMINDING = 23;
        private const int HOURSTOWAITBEFORECANCELLINGINVITATION = 4;
        // ReSharper restore InconsistentNaming

        public List<Invitation> OutstandingInvites => _activeInvitations;

        public PizzaInviter(ILogger logger, BotConfig botConfig, IFileStorage storage, IPizzaCore core, IActivityLog activityLog)
        {
            _activityLog = activityLog ?? throw new ArgumentNullException(nameof(activityLog));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _botConfig = botConfig ?? throw new ArgumentNullException(nameof(botConfig));
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _core = core ?? throw new ArgumentNullException(nameof(core));
        }

        public async Task Start()
        {          
            if(_core == null) throw new ArgumentNullException(nameof(_core));
            _logger.Debug("Starting Pizza Inviter.");

            await _storage.Start();
            _activeInvitations = _storage.ReadFile<Invitation>(INVITESFILE).ToList();
            _timer = new Timer(async _ => await FollowUpInvitesAndReminders(), null, TimeSpan.FromSeconds(10), TimeSpan.FromMinutes(10));
        }

        public async Task Stop()
        {
            await _storage.Stop();
            _timer.Dispose();
            _logger.Information($"{this.GetType().Name} stopped succesfully.");
        }

        public async Task FollowUpInvitesAndReminders()
        {
            try
            {
                await SendInvites();
                await SendReminders();
                await CancelExpiredInvitations();
            }
            catch (Exception e)
            {
                _logger.Fatal(e, "Exception running 'FollowUpInvitesAndReminders'");
            }
        }

        public void Invite(IEnumerable<Invitation> newInvites)
        {
            _activeInvitations.AddRange(newInvites);
            _storage.SaveFile(INVITESFILE, _activeInvitations.ToArray());
        }

        private async Task SendInvites()
        {
            var unsentInvitations = _activeInvitations.Where(i => i.Invited == null).ToList();
            if (!unsentInvitations.Any())
            {   return;}

            _logger.Information("Sending {NumberOfInvites} invites.", unsentInvitations.Count);
            Invitation invitation;
            while ((invitation = unsentInvitations.FirstOrDefault(i => i.Invited == null)) != null)
            {
                var message = invitation.CreateNewInvitationMessage(_botConfig.BotRoom);
                await _core.SendMessage(message);
                invitation.Invited = DateTimeOffset.UtcNow;
                _storage.SaveFile(INVITESFILE, _activeInvitations.ToArray());
                _activityLog.Log($"Sent INVITE to event '{invitation.EventId}' to user {invitation.UserName}");
            }
        }

        private async Task SendReminders()
        {
            var invitationsNotRespondedTo = _activeInvitations
                .Where(i => i.Invited != null && i.Response == Invitation.ResponseEnum.NoResponse).ToList();
            var reminders = invitationsNotRespondedTo
                .Where(i => i.Reminded == null && i.Invited?.AddHours(HOURSTOWAITBEFOREREMINDING) < DateTimeOffset.UtcNow).ToList();
            if (!reminders.Any())
            { return; }

            _logger.Debug("Sending {NumberOfReminders} reminders.", reminders.Count);
            Invitation reminder;
            while ((reminder = reminders.FirstOrDefault(i => i.Reminded == null)) != null)                                                 
            {
                var message = reminder.CreateReminderMessage();
                await _core.SendMessage(message);
                reminder.Reminded = DateTimeOffset.UtcNow;
                _storage.SaveFile(INVITESFILE, _activeInvitations.ToArray());
                _activityLog.Log($"Sent REMINDER to event '{reminder.EventId}' to user {reminder.UserName}");
            }
        }

        private async Task CancelExpiredInvitations()
        {
            var invitationsNotRespondedTo = _activeInvitations
                .Where(i => i.Invited != null && i.Response == Invitation.ResponseEnum.NoResponse).ToList();
            var expirations = invitationsNotRespondedTo
                .Where(i => i.Reminded?.AddHours(HOURSTOWAITBEFORECANCELLINGINVITATION) < DateTimeOffset.UtcNow).ToList();
            if (!expirations.Any())
            {   return; }

            _logger.Information("Sending {NumberOfExpirations} expirations.", expirations.Count);

            Invitation invitation;
            while ((invitation = expirations.FirstOrDefault(i=> i.Response == Invitation.ResponseEnum.NoResponse)) != null)
            {
                var message = invitation.CreateExpiredInvitationMessage();
                await _core.SendMessage(message);
                invitation.Response = Invitation.ResponseEnum.Expired;
                _activeInvitations.Remove(invitation);
                await RaiseOnInvitationChanged(invitation);

                _storage.SaveFile(INVITESFILE, _activeInvitations.ToArray());
                _activityLog.Log($"Sent EXPIRATION to event '{invitation.EventId}' to user {invitation.UserName}");
            }
        }


        public async Task<bool> HandleMessage(IncomingMessage incomingMessage)
        {
            var existingInvitation = _activeInvitations.FirstOrDefault(inv => incomingMessage.UserId == inv.UserId);
            if (existingInvitation?.Response == Invitation.ResponseEnum.NoResponse)
            {
                if(new[] { "yes", "Yes", "YES", "Yes.", "Yes!"}.Contains(incomingMessage.FullText))
                {
                    await AcceptInvitation(incomingMessage);
                    return true;
                }
                else if (new[] {"no", "No", "NO", "No.", "No!"}.Contains(incomingMessage.FullText))
                {
                    await RejectInvitation(incomingMessage);
                    return true;
                }
                else
                {
                    await _core.SendMessage(incomingMessage.ReplyDirectlyToUser("I'm sorry, I didn't catch that. Did you mean to accept the pizza invitation? Please reply `yes` or `no`."));
                    return true;
                }
            }
            return false;
        }

        private async Task AcceptInvitation(IncomingMessage incomingMessage)
        {
            var reply = incomingMessage.ReplyDirectlyToUser("Thank you. I will keep you informed when the other guests have accepted!");
            await _core.SendMessage(reply);

            var invitation = _activeInvitations.Single(i => i.UserId == incomingMessage.UserId);
            invitation.Response = Invitation.ResponseEnum.Accepted;
            _activityLog.Log($"User {incomingMessage.Username} ACCEPTED the invitation for event '{invitation.EventId}'");
            await RaiseOnInvitationChanged(invitation);

            _activeInvitations.Remove(invitation);
            _storage.SaveFile(INVITESFILE, _activeInvitations.ToArray());
        }

        private async Task RejectInvitation(IncomingMessage incomingMessage)
        {
            var invitation = _activeInvitations.Single(i => i.UserId == incomingMessage.UserId);
            invitation.Response= Invitation.ResponseEnum.Rejected;
            _activeInvitations.Remove(invitation);
            _activityLog.Log($"User {incomingMessage.Username} REJECTED the invitation for event '{invitation.EventId}'");
            await RaiseOnInvitationChanged(invitation);

            _activeInvitations.Remove(invitation);
            _storage.SaveFile(INVITESFILE, _activeInvitations.ToArray());

            var reply = incomingMessage.ReplyDirectlyToUser("That is too bad, I will try to find someone else.");
            await _core.SendMessage(reply);
        }


        public delegate Task InvitationChangedEventHandler(Invitation invitation);
        public event InvitationChangedEventHandler OnInvitationChanged;
        private async Task RaiseOnInvitationChanged(Invitation invitation)
        {
            var ev = OnInvitationChanged;
            if (ev != null)
            {
                await ev(invitation);
            }
        }
    }
}