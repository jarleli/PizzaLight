using System;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using PizzaLight.Models;
using PizzaLight.Models.SlackModels;
using PizzaLight.Resources;
using PizzaLight.Tests.Harness;
using SlackAPI.WebSocketMessages;

namespace PizzaLight.Tests.Scenario.Planner
{
    [TestFixture, Category("Unit")]
    public class GetsFourConfirmedGuestsAndReachesDeadline
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
                await _harness.Inviter.HandleMessage(new NewMessage() { user = person.UserId, text = "yes" });
            }
            _harness.Tick();
            Assert.IsNull(_plan.Cancelled);
            _harness.Core.Invocations.Clear();

            _harness.Now = _harness.Now.AddDays(8);
            _harness.Tick();

            _harness.Logger.Verify(l => l.Fatal(It.IsAny<Exception>(), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void PlanIsNotCancelled()
        {
            Assert.IsNull(_plan.Cancelled);
        }
        [Test]
        public void PlanGetsConfirmed()
        {
            Assert.IsTrue(_plan.ParticipantsLocked);
        }

        [Test]
        public void PlanGetsAnnounced()
        {
            _harness.Core.Verify(c=>c.SendMessage(It.Is<MessageToSend>(
                m=> m.Text.Contains("will get together and eat some tasty pizza"))), Times.Once);
        }

        [Test]
        public void ParticipantsGetInformed()
        {
            _harness.Core.Verify(c=>c.SendMessage(It.Is<MessageToSend>(
                m=> m.Text.Contains("This amazing group of people has accepted the invitation for pizza on"))), Times.Exactly(4));
        }

      

    }
}