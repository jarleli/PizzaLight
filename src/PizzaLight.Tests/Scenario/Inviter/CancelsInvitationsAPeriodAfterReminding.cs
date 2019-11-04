using System;
using System.Linq;
using Moq;
using Noobot.Core.MessagingPipeline.Response;
using NUnit.Framework;
using PizzaLight.Tests.Harness;

namespace PizzaLight.Tests.Scenario.Inviter
{
    [TestFixture, Category("Unit")]
    public class CancelsInvitationsAPeriodAfterReminding
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
            _harness.Now = _harness.Now.AddDays(2);
            _harness.Inviter.PizzaInviterLoopTick().Wait();
            _harness.Core.Invocations.Clear();

            _harness.Now = _harness.Now.AddDays(1);
            _harness.Inviter.PizzaInviterLoopTick().Wait();

        }

        [Test]
        public void InvitaionMessagesWereSent()
        {
            _harness.Core.Verify( c 
                => c.SendMessage(It.Is<ResponseMessage>(m 
                => m.Text.Contains("Sadly, you didn't respond to my invitation and I will now invite someone else instead.")))
                    , Times.Exactly(_harness.Config.InvitesPerEvent));
        }

        [Test]
        public void InvitationsAreNowMarkedAsReminded()
        {
            Assert.That(!_harness.InvitationList.Any());
        }
    }
}