using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Moq;
using PizzaLight.Infrastructure;
using PizzaLight.Models;
using PizzaLight.Resources;
using Serilog;
using SlackConnector;
using SlackConnector.Models;

namespace PizzaLight.Tests.Harness
{
    public class TestHarness
    {
        public Mock<IPizzaCore> Core;
        public PizzaInviter Inviter;
        public Mock<IFileStorage> Storage;
        public Mock<ILogger> Logger;
        public BotConfig Config;
        public PizzaPlanner Planner;
        public Mock<ISlackConnection> Connection;
        //public string Channel;
        public ConcurrentDictionary<string, SlackUser> UserCache;
        public Mock<IActivityLog> Activity;

        public Invitation[] LastSavedInvitationList { get; set; }
        public PizzaPlan[] ActivePizzaPlans { get; set; }
        public PizzaPlan[] OldPizzaPlans { get; set; }


        public static TestHarness CreateHarness()
        {
            var harness = new TestHarness();
            harness
                .WithFiveUsersInCache()
                .AddChannels()
                .HasEmptyListOfPlans()
                .HasEmptyListOfOldPlans()
                .HasNoOutstandingInvites()
                ;
            return harness;
        }



        public TestHarness()
        {
            Config = new BotConfig(){InvitesPerEvent = 5, PizzaRoom = new PizzaRoom(){City = "city", Room="testroom"},BotRoom = "botroom"};
            Core = new Mock<IPizzaCore>();
            Storage = new Mock<IFileStorage>();
            Storage.Setup(s => s.SaveFile< Invitation>(PizzaInviter.INVITESFILE, It.IsAny<Invitation[]>()))
                .Callback<string, Invitation[]>((s,invites)=> LastSavedInvitationList = invites);
            Storage.Setup(s => s.SaveFile<PizzaPlan>(PizzaPlanner.ACTIVEEVENTSFILE, It.IsAny<PizzaPlan[]>()))
                .Callback<string, PizzaPlan[]>((s, plans) => ActivePizzaPlans = plans);
            Storage.Setup(s => s.SaveFile<PizzaPlan>(PizzaPlanner.OLDEVENTSFILE, It.IsAny<PizzaPlan[]>()))
                .Callback<string, PizzaPlan[]>((s, plans) => OldPizzaPlans = plans);

            Logger = new Mock<ILogger>();
            Activity = new Mock<IActivityLog>();
            Connection = new Mock<ISlackConnection>();
            Core.SetupGet(c => c.SlackConnection).Returns(Connection.Object);

            Inviter = new PizzaInviter(Logger.Object, Config,Storage.Object, Core.Object,Activity.Object );
            Planner = new PizzaPlanner(Logger.Object, Config, Storage.Object, Inviter, Core.Object, Activity.Object);
        }


        public TestHarness AddChannels()
        {
            var hubs = new ConcurrentDictionary<string, SlackChatHub>();
            var members = UserCache.Select(u => u.Value.Id).ToArray();
            hubs.TryAdd(Config.PizzaRoom.Room, new SlackChatHub() { Id = "123", Name = "#" + Config.PizzaRoom.Room, Members = members , Type = SlackChatHubType.Channel });
            Connection.SetupGet(c => c.ConnectedHubs).Returns(hubs);

            return this;
        }

        public TestHarness WithFiveUsersInCache()
        {
            UserCache = new ConcurrentDictionary<string, SlackUser>();
            UserCache.TryAdd("user1", new SlackUser() { Name = "user1", Id = "id1" });
            UserCache.TryAdd("user2", new SlackUser() { Name = "user2", Id = "id2" });
            UserCache.TryAdd("user3", new SlackUser() { Name = "user3", Id = "id3" });
            UserCache.TryAdd("user4", new SlackUser() { Name = "user4", Id = "id4" });
            UserCache.TryAdd("user5", new SlackUser() { Name = "user5", Id = "id5" });
            Core.Setup(c => c.UserCache).Returns(UserCache);
            return this;
        }


        public TestHarness HasEmptyListOfPlans()
        {
            Storage.Setup(s => s.ReadFile<PizzaPlan>(PizzaPlanner.ACTIVEEVENTSFILE)).Returns(new PizzaPlan[0]);
            return this;
        }

        private TestHarness HasEmptyListOfOldPlans()
        {
            Storage.Setup(s => s.ReadFile<PizzaPlan>(PizzaPlanner.OLDEVENTSFILE)).Returns(new PizzaPlan[0]);
            OldPizzaPlans = new PizzaPlan[0];
            return this;
        }

        public TestHarness HasUpcomingPizzaPlans(PizzaPlan[] pizzaPlans)
        {
            Storage.Setup(s => s.ReadFile<PizzaPlan>(PizzaPlanner.ACTIVEEVENTSFILE)).Returns(pizzaPlans);
            return this;
        }

        private TestHarness HasNoOutstandingInvites()
        {
            Storage.Setup(s => s.ReadFile<Invitation>(PizzaInviter.INVITESFILE)).Returns(new Invitation[0]);

            return this;
        }

        public TestHarness WithFiveUnsentInvitations()
        {
            var invitations = Connection.Object.ConnectedHubs.Single().Value.Members
                .Select(m => new Invitation() {EventId = "a b c", Room = Config.PizzaRoom.Room, UserId = m});
            Inviter.Invite(invitations);
            return this;
        }

    }
}