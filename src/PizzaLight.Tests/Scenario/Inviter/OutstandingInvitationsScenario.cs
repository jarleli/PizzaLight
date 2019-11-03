using System;
using System.Linq;
using Moq;
using Noobot.Core.MessagingPipeline.Response;
using NUnit.Framework;
using PizzaLight.Tests.Harness;

namespace PizzaLight.Tests.Scenario.Inviter
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


            _harness.Inviter.PizzaInviterLoopTick().Wait();
            _harness.Logger.Verify(l=>l.Fatal(It.IsAny<Exception>(), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void InvitaionMessagesWereSent()
        {
            foreach (var invitation in _harness.InvitationList)
            {
                _harness.Core.Verify(
                    c => c.SendMessage(It.Is<ResponseMessage>(m => 
                    m.UserId == invitation.UserId
                    && m.Text.Contains("Do you want to meet up for a social gathering and eat some tasty pizza with other colleagues in")
                    )), Times.Once());
            }
            _harness.Core.Verify(
                c => c.SendMessage(It.Is<ResponseMessage>(m => _harness.UserCache.Values.Any(u=>u.Id == m.UserId))), Times.Exactly(_harness.Config.InvitesPerEvent));

        }

        [Test]
        public void InvitationsAreNowMarkedAsSent()
        {
            Assert.That(_harness.InvitationList.Any());
            Assert.That(_harness.InvitationList.Length == _harness.Config.InvitesPerEvent);
            Assert.That(_harness.InvitationList.All(i => i.Invited != null));
            Assert.That(_harness.InvitationList.All(i => i.Reminded == null));
        }
    }
}