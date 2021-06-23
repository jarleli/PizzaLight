using Moq;
using NUnit.Framework;
using PizzaLight.Models;
using PizzaLight.Models.SlackModels;
using PizzaLight.Resources;
using PizzaLight.Tests.Harness;
using SlackAPI.WebSocketMessages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PizzaLight.Tests.Unit
{
    [TestFixture, Category("Unit")]

    public class OptOutHandlerTests
    {
        private TestHarness _harness;
        private Person _user;

        [SetUp]
        public void Setup()
        {
            _user = new Person() { UserId = "1", UserName = "name1" };
            _harness = TestHarness.CreateHarness();
        }

        [Test]
        public async Task SendingSomeOtherMessageDoesNotResultInAction()
        {
            var res = await _harness.OptOut.HandleMessage(new NewMessage { username = "1", text = "yes" });
            var res2 = await _harness.OptOut.HandleMessage(new NewMessage { user = "1", text = "no" });
            Assert.IsFalse(res);
            Assert.IsFalse(res2);
            _harness.Core.Verify(c => c.SendMessage(It.IsAny<MessageToSend>()), Times.Never);
        }

        [Test]
        public async Task SendingOptOutTriggersReturnsSelectionPrompt()
        {
            var res = await _harness.OptOut.HandleMessage(new NewMessage { user = "1", text = "opt out" });
            Assert.IsTrue(res);
            _harness.Core.Verify(c => c.SendMessage(It.Is<MessageToSend>(m=>m.Text.Contains("Use `opt out [Channel]`, where options for Channel include: "))), Times.Once);
        }

        [Test]
        public async Task SendsOptOutForChannel()
        {
            var res = await _harness.OptOut.HandleMessage(new NewMessage { user = "1", text = "opt out testroom" });
            Assert.IsTrue(res);
            //is asked to confirm
            _harness.Core.Verify(c => c.SendMessage(It.Is<MessageToSend>(m => m.Text.Contains("Please confirm your choice by repeating `opt out"))), Times.Once);
        }

        [Test]
        public async Task SendsOptOutForChannelWithHashInChannelName()
        {
            var res = await _harness.OptOut.HandleMessage(new NewMessage { user = "1", text = "opt out #testroom" });
            Assert.IsTrue(res);
            //is asked to confirm
            _harness.Core.Verify(c => c.SendMessage(It.Is<MessageToSend>(m => m.Text.Contains("Please confirm your choice by repeating `opt out"))), Times.Once);
        }

        [Test]
        public async Task ConfirmsOptOutForChannel()
        {
            var res = await _harness.OptOut.HandleMessage(new NewMessage { user = "1", text = "opt out testroom" });
            Assert.IsTrue(res);
            //is asked to confirm
            _harness.Core.Verify(c => c.SendMessage(It.Is<MessageToSend>(m => m.Text.Contains("Please confirm your choice by repeating `opt out"))), Times.Once);

            res = await _harness.OptOut.HandleMessage(new NewMessage { user = "1", text = "opt out testroom" });
            Assert.IsTrue(res);
            _harness.OptOutState.Verify(s => s.AddUserToOptOutOfChannel(It.Is<Person>(p => p.UserId == _user.UserId), "testroom"), Times.Once);
            _harness.Core.Verify(c => c.SendMessage(It.Is<MessageToSend>(m => m.Text.Contains("You have now opted out of any and all upcoming pizza plans in the channel"))), Times.Once);
            Assert.IsTrue(_harness.OptOutState.Object.ChannelList["testroom"].UsersThatHaveOptedOut.Any(u => u.UserId == _user.UserId));
        }

        [Test]
        public async Task ConfirmsOptOutForChannelAfterAnHourAsksForNewConfirmation()
        {
            var res = await _harness.OptOut.HandleMessage(new NewMessage { user = "1", text = "opt out testroom" });
            Assert.IsTrue(res);
            //is asked to confirm
            _harness.Core.Verify(c => c.SendMessage(It.Is<MessageToSend>(m => m.Text.Contains("Please confirm your choice by repeating `opt out"))), Times.Once);
            _harness.Now = _harness.Now.AddHours(1);


            res = await _harness.OptOut.HandleMessage(new NewMessage { user = "1", text = "opt out testroom" });
            _harness.OptOutState.Verify(s => s.AddUserToOptOutOfChannel(It.Is<Person>(p => p.UserId == _user.UserId), "testroom"), Times.Never);
            _harness.Core.Verify(c => c.SendMessage(It.Is<MessageToSend>(m => m.Text.Contains("Please confirm your choice by repeating `opt out"))), Times.Exactly(2));
        }

        [Test]
        public async Task UserCanOptInAfterOptingOut()
        {
            await _harness.OptOut.HandleMessage(new NewMessage { user = "1", text = "opt out testroom" });
            await _harness.OptOut.HandleMessage(new NewMessage { user = "1", text = "opt out testroom" });
            Assert.IsTrue(_harness.OptOutState.Object.ChannelList["testroom"].UsersThatHaveOptedOut.Any(u => u.UserId == "1"));
            
            await _harness.OptOut.HandleMessage(new NewMessage { user = "1", text = "opt in testroom" });
            _harness.OptOutState.Verify(s => s.RemoveUserFromOptOutOfChannel(It.Is<Person>(p => p.UserId == _user.UserId), "testroom"), Times.Once);
            Assert.IsFalse(_harness.OptOutState.Object.ChannelList["testroom"].UsersThatHaveOptedOut.Any(u => u.UserId == "1"));
        }
    }
}
