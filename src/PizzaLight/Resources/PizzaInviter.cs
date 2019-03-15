using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Noobot.Core.MessagingPipeline.Request;
using Noobot.Core.MessagingPipeline.Response;
using Serilog;

namespace PizzaLight.Resources
{
    public class PizzaInviter : IMessageHandler, IMustBeInitialized
    {
        private readonly ILogger _logger;
        private readonly JsonStorage _storage;
        private PizzaCore _core;
        private readonly List<Invitation> _activeInvitations = new List<Invitation>();

        private const string INVITEFILES = "pendinginvites";


        public PizzaInviter(ILogger logger, JsonStorage storage, PizzaCore core)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _core = core ?? throw new ArgumentNullException(nameof(core));
        }

    

        public async Task Start()
        {          
            if(_core == null) throw new ArgumentNullException(nameof(_core));

            await _storage.Start();
            SendInvites();
        }

        public async Task Stop()
        {
            await _storage.Stop();
            _logger.Information($"{this.GetType().Name} stopped succesfully.");
        }


        public void AddToInviteList(IEnumerable<Invitation> newInvites)
        {
            _activeInvitations.AddRange(newInvites);
            _storage.SaveFile(INVITEFILES, _activeInvitations.ToArray());
            SendInvites();
        }

        private void SendInvites()
        {
            _logger.Debug("{NumberOfInvites} invites to send.", _activeInvitations.Count);
            while (_activeInvitations.Any(i => i.Invited == false))
            {
                var toInvite = _activeInvitations.First(i => i.Invited == false);
                var user = _core.SlackConnection.UserCache.Values.SingleOrDefault(u => u.Name == toInvite.UserName);
                if (user != null)
                {
                    var message = new ResponseMessage()
                    {
                        Text =
                            $"Do you want to meet up for a social gathering and eat some tasty pizza with other colleagues on *{toInvite.EventTime.LocalDateTime:dddd, MMMM dd} at {toInvite.EventTime.LocalDateTime:HH:mm}*? \n" +
                            "Five other random colleagues have also been invited, and if you want to get to know them better all you have to do is reply yes if you want to accept this invitation or no if you can't make it and I will invite someone else in your stead. \n" +
                            "Please reply `yes` or `no`.",

                        ResponseType = ResponseType.DirectMessage,
                        UserId = user.Id,
                    };
                    _core.SendMessage(message).Wait();
                    toInvite.Invited = true;
                    _storage.SaveFile(INVITEFILES, _activeInvitations.ToArray());
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
            if (_activeInvitations.Any(inv => incomingMessage.UserId == inv.UserId))
            {
                if (incomingMessage.FullText == "yes")
                {
                    await AcceptInvitation(incomingMessage);
                }
                if (incomingMessage.FullText == "no")
                {
                    //reject();
                }
            }
        }

        private async Task AcceptInvitation(IncomingMessage incomingMessage)
        {
            var invitation = _activeInvitations.Single(i => i.UserId == incomingMessage.UserId);
            invitation.Accepted = true;
            //notify planner?
            _storage.SaveFile(INVITEFILES, _activeInvitations.ToArray());
            _logger.Information("User {UserName} accepted the invitation for event.", incomingMessage.Username);

            var reply = incomingMessage.ReplyDirectlyToUser("Thank you. I will keep you informed when the other guests have accepted!");
            await _core.SendMessage(reply);
        }
    }
}