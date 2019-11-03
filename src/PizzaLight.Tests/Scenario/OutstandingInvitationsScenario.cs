using System;
using System.Collections.Generic;
using System.Linq;
using Moq;
using Noobot.Core.MessagingPipeline.Response;
using NUnit.Framework;
using PizzaLight.Models;
using PizzaLight.Resources;
using PizzaLight.Tests.Harness;
using PizzaLight.Tests.Unit;
using Serilog.Core;

namespace PizzaLight.Tests.Scenario
{
    [TestFixture, Category("Unit")]
    public class OutstandingInvitationsScenario
    {
        private TestHarness _harness;

        [OneTimeSetUp]
        public void SetUp()
        {
            _harness = TestHarness.CreateHarness();
            
            
            _harness.Planner.Start().Wait();
            _harness.Inviter.Start().Wait();
            _harness.WithFiveUnsentInvitations();


            _harness.Inviter.FollowUpInvitesAndReminders().Wait();
            _harness.Logger.Verify(l=>l.Fatal(It.IsAny<Exception>(), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void InvitaionMessagesWereSent()
        {
            _harness.Core.Verify(
                c => c.SendMessage(It.Is<ResponseMessage>(m => _harness.UserCache.Values.Any(u=>u.Id == m.UserId))), Times.Exactly(_harness.Config.InvitesPerEvent));
        }

        [Test]
        public void InvitationsAreNowMarkedAsSent()
        {
            Assert.That(_harness.LastSavedInvitationList.Any());
            Assert.That(_harness.LastSavedInvitationList.Length == _harness.Config.InvitesPerEvent);
            Assert.That(_harness.LastSavedInvitationList.All(i => i.Invited != null));
        }
    }
}