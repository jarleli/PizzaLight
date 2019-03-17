using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using Noobot.Core.MessagingPipeline.Request;
using Noobot.Core.MessagingPipeline.Response;
using PizzaLight.Models;
using Serilog;

namespace PizzaLight.Resources
{
    public class PizzaInviter : IPizzaInviter
    {
        private readonly ILogger _logger;
        private readonly IFileStorage _storage;
        private IPizzaCore _core;
        private List<Invitation> _activeInvitations;

        private const string INVITESFILE = "active invites";


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
            await SendInvites();
        }

        public async Task Stop()
        {
            await _storage.Stop();
            _logger.Information($"{this.GetType().Name} stopped succesfully.");
        }


        public async Task Invite(IEnumerable<Invitation> newInvites)
        {
            _activeInvitations.AddRange(newInvites);
            _storage.SaveFile(INVITESFILE, _activeInvitations.ToArray());
            await SendInvites();
        }

        private async Task SendInvites()
        {
            _logger.Debug("{NumberOfInvites} invites to send.", _activeInvitations.Count);
            while (_activeInvitations.Any(i => i.Invited == false))
            {
                var toInvite = _activeInvitations.First(i => i.Invited == false);
                var user = _core.UserCache.Values.SingleOrDefault(u => u.Name == toInvite.UserName);
                if (user != null)
                {
                    var day = toInvite.EventTime.LocalDateTime.ToString("dddd, MMMM dd");
                    var time = toInvite.EventTime.LocalDateTime.ToString("HH:mm");
                    var message = new ResponseMessage()
                    {
                        Text =
                            $"Do you want to meet up for a social gathering and eat some tasty pizza with other colleagues on *{day} at {time}*? \n" +
                            "Four other random colleagues have also been invited, and if you want to get to know them better all you have to do is reply yes if you want to accept this invitation or no if you can't make it and I will invite someone else in your stead. \n" +
                            "Please reply `yes` or `no`.",

                        ResponseType = ResponseType.DirectMessage,
                        UserId = user.Id,
                    };
                    await _core.SendMessage(message);
                    toInvite.Invited = true;
                    _storage.SaveFile(INVITESFILE, _activeInvitations.ToArray());
                    _logger.Information("Sent invite to user {UserName}", toInvite.UserName);
                }
                else
                {
                    _logger.Warning("Could not send message because could not find user {UserName}", toInvite.UserName);
                }
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
                else if (incomingMessage.FullText == "yes" || incomingMessage.FullText == "no")
                {
                    var availability = (invitation.Response == Invitation.ResponseEnum.Accepted)
                        ? "attending"
                        : "unavailable";
                    var reply = incomingMessage.ReplyDirectlyToUser(
                        $"I already marked you down as {availability}. Have a nice day.");
                    await _core.SendMessage(reply);
                }
                
            }
        }

        private async Task AcceptInvitation(IncomingMessage incomingMessage)
        {
            var reply = incomingMessage.ReplyDirectlyToUser("Thank you. I will keep you informed when the other guests have accepted!");
            await _core.SendMessage(reply);

            var invitation = _activeInvitations.Single(i => i.UserId == incomingMessage.UserId);
            invitation.Response = Invitation.ResponseEnum.Accepted;
            await RaiseOnInvitationChanged(invitation);

            _storage.SaveFile(INVITESFILE, _activeInvitations.ToArray());
            _logger.Information("User {UserName} accepted the invitation for event.", incomingMessage.Username);
        }

        private async Task RejectInvitation(IncomingMessage incomingMessage)
        {
            var reply = incomingMessage.ReplyDirectlyToUser("That is too bad, I will try to find someone else.");
            await _core.SendMessage(reply);

            var invitation = _activeInvitations.Single(i => i.UserId == incomingMessage.UserId);
            invitation.Response= Invitation.ResponseEnum.Rejected;
            await RaiseOnInvitationChanged(invitation);

            _storage.SaveFile(INVITESFILE, _activeInvitations.ToArray());
            _logger.Information("User {UserName} rejected the invitation for event.", incomingMessage.Username);


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
}