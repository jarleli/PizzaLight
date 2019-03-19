using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Noobot.Core.MessagingPipeline.Request;
using Noobot.Core.MessagingPipeline.Response;
using PizzaLight.Models;
using Serilog;
using SlackConnector.Models;

namespace PizzaLight.Resources
{
    
    public class PizzaInviter : IPizzaInviter
    {
        private readonly ILogger _logger;
        private readonly IFileStorage _storage;
        private readonly IPizzaCore _core;
        private List<Invitation> _activeInvitations;
        private Timer _timer;

        // ReSharper disable InconsistentNaming
        private const string INVITESFILE = "active invites";
        private const int HOURSTOWAITBEFOREREMINDING = 23;
        private const int HOURSTOWAITBEFORECANCELLINGINVITATION = 25;
        // ReSharper restore InconsistentNaming


        public PizzaInviter(ILogger logger, IFileStorage storage, IPizzaCore core)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _core = core ?? throw new ArgumentNullException(nameof(core));
        }

        public async Task Start()
        {          
            if(_core == null) throw new ArgumentNullException(nameof(_core));

            await _storage.Start();
            _activeInvitations = _storage.ReadFile<Invitation>(INVITESFILE).ToList();
            _timer = new Timer(async state => await SendInvitationsAndReminders(state), null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(60));
        }

        public async Task Stop()
        {
            await _storage.Stop();
            _timer.Dispose();
            _logger.Information($"{this.GetType().Name} stopped succesfully.");
        }

        private async Task SendInvitationsAndReminders(object state)
        {
            await SendInvites();
            await SendReminders();
            await CancelOldInvitations();
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
                var message = invitation.CreateNewInvitationMessage();
                await _core.SendMessage(message);
                invitation.Invited = DateTimeOffset.UtcNow;
                _storage.SaveFile(INVITESFILE, _activeInvitations.ToArray());
                _logger.Information("Sent invite to user {UserName}", invitation.UserName);
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

            _logger.Information("Sending {NumberOfReminders} reminders.", reminders.Count);

            Invitation reminder;
            while ((reminder = reminders.FirstOrDefault(i => i.Reminded == null)) != null)                                                 
            {
                var message = reminder.CreateReminderMessage();
                await _core.SendMessage(message);
                reminder.Reminded = DateTimeOffset.UtcNow;
                _storage.SaveFile(INVITESFILE, _activeInvitations.ToArray());
                _logger.Information("Sent reminder to user {UserName}", reminder.UserName);
            }
        }


        private async Task CancelOldInvitations()
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
                _logger.Information("Sent expiration to user {UserName}", invitation.UserName);
            }
        }

        


        public async Task HandleMessage(IncomingMessage incomingMessage)
        {
            var invitation = _activeInvitations.FirstOrDefault(inv => incomingMessage.UserId == inv.UserId);
            if (invitation != null)
            {
                if (invitation.Response == Invitation.ResponseEnum.NoResponse)
                {
                    if (incomingMessage.FullText == "yes")
                    {
                        await AcceptInvitation(incomingMessage);
                    }
                    if (incomingMessage.FullText == "no")
                    {
                        await RejectInvitation(incomingMessage);
                    }
                }
            }
        }

        private async Task AcceptInvitation(IncomingMessage incomingMessage)
        {

            var invitation = _activeInvitations.Single(i => i.UserId == incomingMessage.UserId);
            invitation.Response = Invitation.ResponseEnum.Accepted;
            await RaiseOnInvitationChanged(invitation);

            _storage.SaveFile(INVITESFILE, _activeInvitations.ToArray());
            _logger.Information("User {UserName} accepted the invitation for event.", incomingMessage.Username);

            var reply = incomingMessage.ReplyDirectlyToUser("Thank you. I will keep you informed when the other guests have accepted!");
            await _core.SendMessage(reply);
        }

        private async Task RejectInvitation(IncomingMessage incomingMessage)
        {
            var invitation = _activeInvitations.Single(i => i.UserId == incomingMessage.UserId);
            invitation.Response= Invitation.ResponseEnum.Rejected;
            _activeInvitations.Remove(invitation);
            await RaiseOnInvitationChanged(invitation);

            _storage.SaveFile(INVITESFILE, _activeInvitations.ToArray());
            _logger.Information("User {UserName} rejected the invitation for event.", incomingMessage.Username);

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
                try
                {
                    await ev(invitation);
                }
                catch (Exception ex
                )
                {
                    _logger.Error(ex, "Error Raising OnAcceptedInvitation.");
                }
            }
        }
    }

    public static class ResponseMessageExtensions
    {
        public static ResponseMessage CreateNewInvitationMessage(this Invitation invitation)
        {
            var day = invitation.EventTime.LocalDateTime.ToString("dddd, MMMM dd");
            var time = invitation.EventTime.LocalDateTime.ToString("HH:mm");
            var message = new ResponseMessage()
            {
                Text =
                    $"Hello @{invitation.UserName}. \n" +
                    $"Do you want to meet up for a social gathering and eat some tasty pizza with other colleagues on *{day} at {time}*? \n" +
                    "Four other random colleagues have also been invited, and if you want to get to know them better all you have to do is reply yes if you want to accept this invitation or no if you can't make it and I will invite someone else in your stead. \n" +
                    "Please reply `yes` or `no`.",

                ResponseType = ResponseType.DirectMessage,
                UserId = invitation.UserId,
            };
            return message;
        }
        public static ResponseMessage CreateReminderMessage(this Invitation reminder)
        {
            var day = reminder.EventTime.LocalDateTime.ToString("dddd, MMMM dd");
            var time = reminder.EventTime.LocalDateTime.ToString("HH:mm");
            var message = new ResponseMessage()
            {
                Text =
                    $"Hello @{reminder.UserName}. I recently sent you an invitation for a social pizza event on *{day} at {time}*. \n" +
                    "Since you haven't responded yet I'm sending you this friendly reminder. If you don't respond before tomorrow I will assume that you cannot make it and will invite someone else instead. \n" +
                    "Please reply `yes` or `no` to indicate whether you can make it..",

                ResponseType = ResponseType.DirectMessage,
                UserId = reminder.UserId,
            };
            return message;
        }

        public static ResponseMessage CreateExpiredInvitationMessage(this Invitation invitation)
        {
            var message = new ResponseMessage()
            {
                Text =
                    $"Hello @{invitation.UserName}." +
                    $"Sadly, you didn't respond to my invitation and I will now invite someone else instead." +
                    $"Maybe we will have better luck sometime later.",

                ResponseType = ResponseType.DirectMessage,
                UserId = invitation.UserId,
            };
            return message;
        }
    }
}