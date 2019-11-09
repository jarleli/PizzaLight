using Moq;
using Noobot.Core.MessagingPipeline.Response;
using NUnit.Framework;
using PizzaLight.Resources;
using PizzaLight.Tests.Harness;
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


        [SetUp]
        public void Setup()
        {
            _harness = TestHarness.CreateHarness();
        }

        [Test]
        public async Task SendingSomeOtherMessageDoesNotResultInAction()
        {
            var res = await _harness.OptOut.HandleMessage(new Noobot.Core.MessagingPipeline.Request.IncomingMessage { UserId = "1", FullText = "yes" });
            var res2 = await _harness.OptOut.HandleMessage(new Noobot.Core.MessagingPipeline.Request.IncomingMessage { UserId = "1", FullText = "no" });
            Assert.IsFalse(res);
            Assert.IsFalse(res2);
            _harness.Core.Verify(c => c.SendMessage(It.IsAny<ResponseMessage>()), Times.Never);
        }

        [Test]
        public async Task SendingOptOutTriggersReturnsSelectionPrompt()
        {
            var res = await _harness.OptOut.HandleMessage(new Noobot.Core.MessagingPipeline.Request.IncomingMessage { UserId = "1", FullText = "opt out" });
            Assert.IsTrue(res);
            _harness.Core.Verify(c => c.SendMessage(It.Is<ResponseMessage>(m=>m.Text.Contains("Use `opt out` `[Channel]`, where options for Channel include: "))), Times.Once);
        }

        [Test]
        public async Task SendsOptOutForChannel()
        {
            var res = await _harness.OptOut.HandleMessage(new Noobot.Core.MessagingPipeline.Request.IncomingMessage { UserId = "1", FullText = "opt out testroom" });
            Assert.IsTrue(res);
            //is asked to confirm
            _harness.Core.Verify(c => c.SendMessage(It.Is<ResponseMessage>(m => m.Text.Contains("Please confirm your choice by repeating `opt out"))), Times.Once);
        }

        [Test]
        public async Task ConfirmsOptOutForChannelWithin30Seconds()
        {
            var res = await _harness.OptOut.HandleMessage(new Noobot.Core.MessagingPipeline.Request.IncomingMessage { UserId = "1", FullText = "opt out testroom" });
            Assert.IsTrue(res);
            //is asked to confirm
            _harness.Core.Verify(c => c.SendMessage(It.Is<ResponseMessage>(m => m.Text.Contains("Please confirm your choice by repeating `opt out"))), Times.Once);
            _harness.Now = _harness.Now.AddSeconds(40);


            res = await _harness.OptOut.HandleMessage(new Noobot.Core.MessagingPipeline.Request.IncomingMessage { UserId = "1", FullText = "opt out testroom" });
            _harness.OptOutState.Verify(s => s.AddUserToOptOutOfChannel("1", "testroom"), Times.Never);

            res = await _harness.OptOut.HandleMessage(new Noobot.Core.MessagingPipeline.Request.IncomingMessage { UserId = "1", FullText = "opt out testroom" });
            Assert.IsTrue(res);
            _harness.OptOutState.Verify(s => s.AddUserToOptOutOfChannel("1", "testroom"), Times.Once);
            _harness.Core.Verify(c => c.SendMessage(It.Is<ResponseMessage>(m => m.Text.Contains("You have now opted out of all and any upcoming pizza plans in the channel"))), Times.Once);
            Assert.IsTrue(_harness.OptOutState.Object.ChannelList["testroom"].UsersThatHaveOptedOut.Any(u => u == "1"));
        }


        [Test]
        public async Task UserCanOptInAfterOptingOut()
        {
            await _harness.OptOut.HandleMessage(new Noobot.Core.MessagingPipeline.Request.IncomingMessage { UserId = "1", FullText = "opt out testroom" });
            await _harness.OptOut.HandleMessage(new Noobot.Core.MessagingPipeline.Request.IncomingMessage { UserId = "1", FullText = "opt out testroom" });
            Assert.IsTrue(_harness.OptOutState.Object.ChannelList["testroom"].UsersThatHaveOptedOut.Any(u => u =="1"));
            
            await _harness.OptOut.HandleMessage(new Noobot.Core.MessagingPipeline.Request.IncomingMessage { UserId = "1", FullText = "opt in testroom" });
            _harness.OptOutState.Verify(s => s.RemoveUserFromOptOutOfChannel("1", "testroom"), Times.Once);
            Assert.IsFalse(_harness.OptOutState.Object.ChannelList["testroom"].UsersThatHaveOptedOut.Any(u => u == "1"));
        }
    }
}
