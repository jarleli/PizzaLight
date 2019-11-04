using System;
using System.Linq;
using Moq;
using Noobot.Core.MessagingPipeline.Response;
using NUnit.Framework;
using PizzaLight.Tests.Harness;

namespace PizzaLight.Tests.Scenario.Inviter
{
    [TestFixture, Category("Unit")]
    public class SendsRemindersAfter24Hours
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
            _harness.Core.Invocations.Clear();

            _harness.Now = _harness.Now.AddDays(2);
            _harness.Inviter.PizzaInviterLoopTick().Wait();
        }

        [Test]
        public void InvitaionMessagesWereSent()
        {
            foreach (var invitation in _harness.InvitationList)
            {
                _harness.Core.Verify(
                    c => c.SendMessage(It.Is<ResponseMessage>(m => m.UserId == invitation.UserId
                    && m.Text.Contains("I recently sent you an invitation for a social pizza event on ")))
                    , Times.Once());
            }

        }

        [Test]
        public void InvitationsAreNowMarkedAsReminded()
        {
            Assert.That(_harness.InvitationList.Any());
            Assert.That(_harness.InvitationList.Length == _harness.Config.InvitesPerEvent);
            Assert.That(_harness.InvitationList.All(i => i.Invited != null));
            Assert.That(_harness.InvitationList.All(i => i.Reminded != null));
        }
    }
}