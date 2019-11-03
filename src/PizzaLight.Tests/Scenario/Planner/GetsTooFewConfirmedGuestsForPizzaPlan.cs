using System;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using Noobot.Core.MessagingPipeline.Request;
using NUnit.Framework;
using PizzaLight.Models;
using PizzaLight.Resources;
using PizzaLight.Tests.Harness;

namespace PizzaLight.Tests.Scenario.Planner
{
    [TestFixture, Category("Unit")]
    public class GetsTooFewConfirmedGuestsForPizzaPlan
    {
        private TestHarness _harness;
        private PizzaPlan _plan;

        [OneTimeSetUp]
        public void SetUp()
        {
            _harness = TestHarness.CreateHarness();
            _harness.Start();

            _harness.Tick();
            _plan = _harness.ActivePizzaPlans.Single();
            //await _harness.Inviter.HandleMessage(new IncomingMessage() {UserId = _plan.Invited.First().UserId, FullText = "yes"});

            _harness.Now = _harness.Now.AddDays(8);
            _harness.Tick();
        }

        [Test]
        public void PlanGetsCancelled()
        {
            Assert.IsNotNull(_plan.Cancelled);
        }

        [Test]
        public void CancelledPlanIsMovedToOldPlans()
        {
            Assert.IsNull(_harness.ActivePizzaPlans.SingleOrDefault(p => p.Id == _plan.Id));
            Assert.IsNotNull(_harness.OldPizzaPlans.SingleOrDefault(p => p.Id == _plan.Id));
        }


    }
}