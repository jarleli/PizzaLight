using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Moq;
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

            
            _userCache = new ConcurrentDictionary<string, SlackUser>();
            _userCache.TryAdd("user1", new SlackUser() { Name = "user1", Id = "id1" });
            _userCache.TryAdd("user2", new SlackUser() { Name = "user2", Id = "id2" });
            _userCache.TryAdd("user3", new SlackUser() { Name = "user3", Id = "id3" });
            _core.Setup(c => c.UserCache).Returns(_userCache);

            _planner = new PizzaPlanner(_logger.Object, _config, _storage.Object, _inviter.Object, _core.Object, _activity.Object);
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
            var dateTime = _planner.GetTimeOfNextEvent();
            Assert.That(dateTime.DayOfWeek != DayOfWeek.Saturday);
            Assert.That(dateTime.DayOfWeek != DayOfWeek.Sunday);
            Assert.That(dateTime>DateTimeOffset.UtcNow);
            Assert.That(dateTime.TimeOfDay>TimeSpan.FromHours(15));
        }
    }
}
