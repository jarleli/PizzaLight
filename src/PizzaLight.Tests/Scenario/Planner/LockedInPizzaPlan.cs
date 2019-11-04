using System;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using Noobot.Core.MessagingPipeline.Request;
using Noobot.Core.MessagingPipeline.Response;
using NUnit.Framework;
using PizzaLight.Models;
using PizzaLight.Resources;
using PizzaLight.Tests.Harness;

namespace PizzaLight.Tests.Scenario.Planner
{
    [TestFixture, Category("Unit")]
    public class LockedInPizzaPlanChosesRolesForGuests
    {
        private TestHarness _harness;
        private PizzaPlan _plan;

        [OneTimeSetUp]
        public async Task SetUp()
        {
            _harness = TestHarness.CreateHarness();
            _harness.Start();

            _harness.Tick();
            _plan = _harness.ActivePizzaPlans.Single();
            foreach (var person in _plan.Invited.Take(4).ToList())
            {
                await _harness.Inviter.HandleMessage(new IncomingMessage() { UserId = person.UserId, FullText = "yes" });
            }
            _harness.Tick();
            Assert.IsNull(_plan.Cancelled);
            _harness.Core.Invocations.Clear();

            _harness.Now = _harness.Now.AddDays(8);
            _harness.Tick();

            _harness.Logger.Verify(l => l.Fatal(It.IsAny<Exception>(), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void OneGuestIsSelecterdToExpense()
        {
            Assert.IsNotNull(_plan.PersonDesignatedToHandleExpenses);
            Assert.IsTrue(_plan.Accepted.Any(a=>a.UserId == _plan.PersonDesignatedToHandleExpenses.UserId));
        }
        [Test]
        public void OneGuestIsSelecterdToReserveTable()
        {
            Assert.IsNotNull(_plan.PersonDesignatedToMakeReservation);
            Assert.IsTrue(_plan.Accepted.Any(a => a.UserId == _plan.PersonDesignatedToMakeReservation.UserId));
        }

    }
}