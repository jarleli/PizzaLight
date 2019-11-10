using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Moq;
using NUnit.Framework;
using PizzaLight.Infrastructure;
using PizzaLight.Resources;
using PizzaLight.Tests.Harness;

namespace PizzaLight.Tests.Integration
{
    [TestFixture, Category("Integration")] //needs disk access and does not do well in high concurrency environments
    public class OptOutStateIntegrationTests
    {
        private OptOutState _state;

        [SetUp]
        public async Task Setup()
        {
            var storage = new JsonStorage(new Mock<Serilog.ILogger>().Object);
            _state = new OptOutState(storage);
            await _state.Start();
            storage.DeleteFile("optouts");
            await _state.Start();
        }
        
        [Test]
        public void CountOfEmptyList()
        {
            var count = _state.ChannelList.Count();

            Assert.AreEqual(0, count);
        }

        [Test]
        public async Task AddingUsersToOptOutState()
        {
            var count = _state.ChannelList.Count();
            Assert.AreEqual(0, count);

            await _state.AddUserToOptOutOfChannel(new Models.Person() { UserId = "1", UserName = "a" }, "testroom");
            count = _state.ChannelList["testroom"].UsersThatHaveOptedOut.Count();
            Assert.AreEqual(1, count);
        }

        [Test]
        public async Task UsersAddedCanThenBeRemoved()
        {
            var storage = new JsonStorage(new Mock<Serilog.ILogger>().Object);
            var state = new OptOutState(storage);
            await state.Start();
            storage.DeleteFile("optouts");
            Assert.AreEqual(0, state.ChannelList.Count());

            await state.AddUserToOptOutOfChannel(new Models.Person() { UserId = "1", UserName = "a" }, "testroom");
            await state.AddUserToOptOutOfChannel(new Models.Person() { UserId = "2", UserName = "b" }, "testroom");

            var count = state.ChannelList["testroom"].UsersThatHaveOptedOut.Count();
            Assert.AreEqual(2, count);
            await state.RemoveUserFromOptOutOfChannel(new Models.Person() { UserId = "2", UserName = "b" }, "testroom");
            count = state.ChannelList["testroom"].UsersThatHaveOptedOut.Count();
            Assert.AreEqual(1, count);
        }


        [Test]
        public async Task RestartingStateWillContinueWhereLeftOff()
        {
            var count = _state.ChannelList.Count();
            Assert.AreEqual(0, count);

            var expected = Guid.NewGuid().ToString();
            await _state.AddUserToOptOutOfChannel(new Models.Person() { UserId = "1", UserName = expected }, "testroom");
            count = _state.ChannelList["testroom"].UsersThatHaveOptedOut.Count();
            Assert.AreEqual(1, count);
            
            
            var storage = new JsonStorage(new Mock<Serilog.ILogger>().Object);
            var newState = new OptOutState(storage);

            await newState.Start();
            count = _state.ChannelList.Count();
            Assert.AreEqual(1, count);
            Assert.AreEqual(expected, _state.ChannelList["testroom"].UsersThatHaveOptedOut.Single(u => u.UserId == "1").UserName);

        }

    }
}