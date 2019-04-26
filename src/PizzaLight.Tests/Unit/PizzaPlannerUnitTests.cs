using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Flurl.Util;
using Moq;
using Noobot.Core.MessagingPipeline.Response;
using NUnit.Framework;
using PizzaLight.Infrastructure;
using PizzaLight.Models;
using PizzaLight.Resources;
using Serilog;
using SlackConnector;
using SlackConnector.Models;

namespace PizzaLight.Tests.Unit
{
    [TestFixture]
    public class PizzaPlannerUnitTests
    {
        private Mock<IPizzaCore> _core;
        private Mock<IPizzaInviter> _inviter;
        private Mock<IFileStorage> _storage;
        private Mock<ILogger> _logger;
        private BotConfig _config;
        private PizzaPlanner _planner;
        private Mock<ISlackConnection> _connection;
        private string _channel;
        private ConcurrentDictionary<string, SlackUser> _userCache;
        private Mock<IActivityLog> _activity;
        private Dictionary<string, PizzaPlan[]> _inMemoryStorage;

        [OneTimeSetUp]
        public void Setup()
        {
            _channel = "pizzalight";
            _config = new BotConfig();
            _core = new Mock<IPizzaCore>();
            _inviter = new Mock<IPizzaInviter>();
            _storage = new Mock<IFileStorage>();
            _logger = new Mock<ILogger>();
            _activity = new Mock<IActivityLog>();
            _connection = new Mock<ISlackConnection>();
            _core.SetupGet(c => c.SlackConnection).Returns(_connection.Object);
            var hubs = new ConcurrentDictionary<string, SlackChatHub>();
            hubs.TryAdd(_channel, new SlackChatHub() {Id = "123", Name = "#"+_channel, Members = new[] { "id1", "id2", "id3" } });
            _connection.SetupGet(c => c.ConnectedHubs).Returns(hubs);

            _inMemoryStorage = new Dictionary<string, PizzaPlan[]>();
            _storage.Setup(s => s.ReadFile<PizzaPlan>(It.IsAny<string>()))
                .Returns<string>((key) => _inMemoryStorage[key]);
            _storage.Setup(s => s.SaveFile<PizzaPlan>(It.IsAny<string>(), It.IsAny<PizzaPlan[]>()))
                .Callback<string, PizzaPlan[]>((key, plans) => _inMemoryStorage[key] = plans);

            _userCache = new ConcurrentDictionary<string, SlackUser>();
            _userCache.TryAdd("user1", new SlackUser() { Name = "user1", Id = "id1" });
            _userCache.TryAdd("user2", new SlackUser() { Name = "user2", Id = "id2" });
            _userCache.TryAdd("user3", new SlackUser() { Name = "user3", Id = "id3" });
            _core.Setup(c => c.UserCache).Returns(_userCache);

            _planner = new PizzaPlanner(_logger.Object, _config, _storage.Object, _inviter.Object, _core.Object, _activity.Object);
        }

        [SetUp]
        public void PerTestSetup()
        {
        }

        [Test]
        public void GetPeopleToInviteReturnsRightNumberOfPeople()
        {
            var result = _planner.FindPeopleToInvite(_channel, 2, new List<Person>());
            Assert.That(result.Count == 2,"result.Count == 2");
            Assert.That(_userCache.Values.Any(u=>u.Name == result[0].UserName));
            Assert.That(_userCache.Values.Any(u=>u.Name == result[1].UserName));
            Assert.That( result[0].UserName != result[1].UserName);
        }

        [Test]
        public void GetDayOfNextEvent_ReturnsSomeWeekDayInTheFuture()
        {
            var dateTime = _planner.GetTimeOfNextEvent(DateTimeOffset.UtcNow.AddDays(7).Date);
            Assert.That(dateTime.DayOfWeek != DayOfWeek.Saturday);
            Assert.That(dateTime.DayOfWeek != DayOfWeek.Sunday);
            Assert.That(dateTime>DateTimeOffset.UtcNow);
            Assert.That(dateTime.TimeOfDay>TimeSpan.FromHours(15));
        }

        [Test]
        public async Task UpcomingEventWithTooFewGuestsIsCancelled()
        {
            var actiePlans = new[]
            {
                new PizzaPlan()
                {
                    Id = Bip39Words.WordGenerator.GetRandomWordString(3),
                    TimeOfEvent = DateTimeOffset.UtcNow.AddDays(3)
                }
            };
            _inMemoryStorage[PizzaPlanner.ACTIVEEVENTSFILE] = actiePlans;
            _inMemoryStorage[PizzaPlanner.OLDEVENTSFILE] = new PizzaPlan[] { };


            await _planner.Start();
            _planner.CancelOrLockEventIfNotFullBeforeDeadline().Wait();


            //performs only one operation to change the plan
            _storage.Verify(s=>s.SaveFile(PizzaPlanner.ACTIVEEVENTSFILE, It.IsAny<PizzaPlan[]>()),Times.Once);
            _storage.Verify(s=>s.SaveFile(PizzaPlanner.OLDEVENTSFILE, It.IsAny<PizzaPlan[]>()),Times.Once);
            _activity.Verify(s=>s.Log(It.IsAny<string>()), Times.Once);

            Assert.That(_inMemoryStorage[PizzaPlanner.ACTIVEEVENTSFILE].Length == 0);
            Assert.That(_inMemoryStorage[PizzaPlanner.OLDEVENTSFILE].Length == 1);
        }

        [Test]
        public async Task UpcomingEventWithEnoughGuestsIsLockedIn()
        {
            var actiePlans = new[]
            {
                new PizzaPlan()
                {
                    Id = Bip39Words.WordGenerator.GetRandomWordString(3),
                    TimeOfEvent = DateTimeOffset.UtcNow.AddDays(3),
                    Accepted = new  List<Person>(4)
                    {
                        new Person(){UserId = "a"},
                        new Person(){UserId = "b"},
                        new Person(){UserId = "c"},
                        new Person(){UserId = "d"},
                    },
                    ParticipantsLocked = false
                }
            };
            _inMemoryStorage[PizzaPlanner.ACTIVEEVENTSFILE] = actiePlans;
            _inMemoryStorage[PizzaPlanner.OLDEVENTSFILE] = new PizzaPlan[] { };


            await _planner.Start();
            _planner.CancelOrLockEventIfNotFullBeforeDeadline().Wait();

            Assert.That(actiePlans.Single().ParticipantsLocked);
            _storage.Verify(s => s.SaveFile(PizzaPlanner.ACTIVEEVENTSFILE, It.IsAny<PizzaPlan[]>()), Times.Once);
            _core.Verify(s => s.SendMessage(It.IsAny<ResponseMessage>()), Times.Exactly(4));
            _activity.Verify(s => s.Log(It.IsAny<string>()), Times.Once);

            Assert.That(_inMemoryStorage[PizzaPlanner.ACTIVEEVENTSFILE].Length == 1);
            Assert.That(_inMemoryStorage[PizzaPlanner.OLDEVENTSFILE].Length == 0);
        }
    }
}
