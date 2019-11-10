using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
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
        public PizzaPlanner Planner;
        public OptOutHandler OptOut;
        public Mock<IFileStorage> Storage;

        public Mock<ILogger> Logger;
        public BotConfig Config;
        public Mock<ISlackConnection> Connection;
        public Mock<IOptOutState> OptOutState;
        public ConcurrentDictionary<string, SlackUser> UserCache;
        public Mock<IActivityLog> Activity;
        public Func<DateTimeOffset> FuncNow { get; set; }
        public DateTimeOffset Now { get; set; } = new DateTimeOffset(2019, 06, 01, 12, 00, 00, 00, TimeSpan.Zero);

        public Invitation[] InvitationList { get; set; }
        public PizzaPlan[] ActivePizzaPlans { get; set; }
        public PizzaPlan[] OldPizzaPlans { get; set; }


        public static TestHarness CreateHarness()
        {
            var harness = new TestHarness();
            harness
                .WithTenUsersInCache()
                .AddChannels()
                .HasEmptyListOfPlans()
                .HasEmptyListOfOldPlans()
                .HasNoOutstandingInvites()
                .WithANewOptOutStateYouCanAddToAndRemoveFrom()
                
                ;
            return harness;
        }



        public TestHarness()
        {
            FuncNow = () => Now;
            Config = new BotConfig(){InvitesPerEvent = 5, PizzaRoom = new PizzaRoom(){City = "city", Room="testroom"},BotRoom = "botroom"};
            Core = new Mock<IPizzaCore>();
            Storage = new Mock<IFileStorage>();
            Storage.Setup(s => s.SaveArray< Invitation>(PizzaInviter.INVITESFILE, It.IsAny<Invitation[]>()))
                .Callback<string, Invitation[]>((s,invites)=> InvitationList = invites);
            Storage.Setup(s => s.SaveArray<PizzaPlan>(PizzaPlanner.ACTIVEEVENTSFILE, It.IsAny<PizzaPlan[]>()))
                .Callback<string, PizzaPlan[]>((s, plans) => ActivePizzaPlans = plans);
            Storage.Setup(s => s.SaveArray<PizzaPlan>(PizzaPlanner.OLDEVENTSFILE, It.IsAny<PizzaPlan[]>()))
                .Callback<string, PizzaPlan[]>((s, plans) => OldPizzaPlans = plans);

            Logger = new Mock<ILogger>();
            Activity = new Mock<IActivityLog>();
            OptOutState = new Mock<IOptOutState>();
            Connection = new Mock<ISlackConnection>();
            Core.SetupGet(c => c.SlackConnection).Returns(Connection.Object);

            OptOut = new OptOutHandler(Logger.Object, Config, OptOutState.Object, Core.Object, Activity.Object, FuncNow);
            Inviter = new PizzaInviter(Logger.Object, Config,Storage.Object, Core.Object, Activity.Object , FuncNow);
            Planner = new PizzaPlanner(Logger.Object, Config, Storage.Object, Inviter, Core.Object, OptOutState.Object, Activity.Object, FuncNow);
        }
        public void Start()
        {
            Planner.Start().Wait();
            Inviter.Start().Wait();
            OptOut.Start().Wait();
        }

        public void Tick()
        {
            Planner.PizzaPlannerLoopTick().Wait();
            Inviter.PizzaInviterLoopTick().Wait();
        }

        public TestHarness AddChannels()
        {
            var hubs = new ConcurrentDictionary<string, SlackChatHub>();
            var members = UserCache.Select(u => u.Value.Id).ToArray();
            hubs.TryAdd(Config.PizzaRoom.Room, new SlackChatHub() { Id = "123", Name = "#" + Config.PizzaRoom.Room, Members = members , Type = SlackChatHubType.Channel });
            Connection.SetupGet(c => c.ConnectedHubs).Returns(hubs);

            return this;
        }

        public TestHarness WithTenUsersInCache()
        {
            UserCache = new ConcurrentDictionary<string, SlackUser>();
            for (int i = 0; i < 10; i++)
            {
                UserCache.TryAdd($"id{i}", new SlackUser() { Name = $"user{i}", Id = $"id{i}" });

            }
            Core.Setup(c => c.UserCache).Returns(UserCache);
            return this;
        }


        public TestHarness HasEmptyListOfPlans()
        {
            Storage.Setup(s => s.ReadArray<PizzaPlan>(PizzaPlanner.ACTIVEEVENTSFILE)).Returns(new PizzaPlan[0]);
            return this;
        }

        private TestHarness HasEmptyListOfOldPlans()
        {
            Storage.Setup(s => s.ReadArray<PizzaPlan>(PizzaPlanner.OLDEVENTSFILE)).Returns(new PizzaPlan[0]);
            OldPizzaPlans = new PizzaPlan[0];
            return this;
        }
        private TestHarness WithANewOptOutStateYouCanAddToAndRemoveFrom()
        {
            var channelList = new ConcurrentDictionary<string, ChannelState>();
            OptOutState.SetupGet(s => s.ChannelList).Returns(channelList);

            OptOutState.Setup(s=>s.AddUserToOptOutOfChannel(It.IsAny<Person>(), It.IsAny<string>()))
               .Callback<Person, string> ((user, chan) =>
               {
                   if (!channelList.ContainsKey(chan))
                   {
                       channelList.TryAdd(chan, new ChannelState());
                   }
                   channelList[chan].UsersThatHaveOptedOut.Add(user);
               }).Returns(Task.CompletedTask);

            OptOutState.Setup(s => s.RemoveUserFromOptOutOfChannel(It.IsAny<Person>(), It.IsAny<string>()))
                .Callback<Person, string>((user, chan) =>
                {
                    if (!channelList.ContainsKey(chan))
                    {
                        channelList.TryAdd(chan, new ChannelState());
                    }
                    channelList[chan].UsersThatHaveOptedOut = new ConcurrentBag<Person>( channelList[chan].UsersThatHaveOptedOut.Where(u => u.UserId != user.UserId));
                }).Returns(Task.CompletedTask);


            return this;
        }

        public TestHarness HasUpcomingPizzaPlans(PizzaPlan[] pizzaPlans)
        {
            Storage.Setup(s => s.ReadArray<PizzaPlan>(PizzaPlanner.ACTIVEEVENTSFILE)).Returns(pizzaPlans);
            return this;
        }

        private TestHarness HasNoOutstandingInvites()
        {
            Storage.Setup(s => s.ReadArray<Invitation>(PizzaInviter.INVITESFILE)).Returns(new Invitation[0]);

            return this;
        }

        public TestHarness WithFiveUnsentInvitations()
        {
            var invitations = Connection.Object.ConnectedHubs.Single().Value.Members
                .Take(Config.InvitesPerEvent)
                .Select(m => new Invitation() {EventId = "a b c", Room = Config.PizzaRoom.Room, UserId = m});
            Inviter.Invite(invitations);
            return this;
        }

       
    }
}