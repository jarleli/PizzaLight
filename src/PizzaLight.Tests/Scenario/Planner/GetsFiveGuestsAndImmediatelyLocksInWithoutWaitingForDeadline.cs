using System;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using PizzaLight.Models;
using PizzaLight.Models.SlackModels;
using PizzaLight.Resources;
using PizzaLight.Tests.Harness;

namespace PizzaLight.Tests.Scenario.Planner
{
    [TestFixture, Category("Unit")]
    public class GetsFiveGuestsAndImmediatelyLocksInWithoutWaitingForDeadline
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
            foreach (var person in _plan.Invited.ToList())
            {
                await _harness.Inviter.HandleMessage(new SlackAPI.WebSocketMessages.NewMessage() { user = person.UserId, text = "yes" });
            }
            _harness.Core.Invocations.Clear();

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
            _harness.Core.Verify(c => c.SendMessage(It.Is<MessageToSend>(
                m => m.Text.Contains("will get together and eat some tasty pizza"))), Times.Once);
        }

        [Test]
        public void ParticipantsGetInformed()
        {
            _harness.Core.Verify(c => c.SendMessage(It.Is<MessageToSend>(
                m => m.Text.Contains("This amazing group of people has accepted the invitation for pizza on"))), Times.Exactly(5));
        }


        [Test]
        public void OneGuestIsSelecterdToExpense()
        {
            Assert.IsNotNull(_plan.PersonDesignatedToHandleExpenses);
            Assert.IsTrue(_plan.Accepted.Any(a => a.UserId == _plan.PersonDesignatedToHandleExpenses.UserId));
            _harness.Core.Verify(c => c.SendMessage(It.Is<MessageToSend>(
                m => m.UserId == _plan.PersonDesignatedToHandleExpenses.UserId 
                && m.Text.Contains("I need someone to help me handle the expenses for the upcoming pizza dinner"))), Times.Once);
        }

        [Test]
        public void OneGuestIsSelecterdToMakeReservation()
        {
            Assert.IsNotNull(_plan.PersonDesignatedToMakeReservation);
            Assert.IsTrue(_plan.Accepted.Any(a => a.UserId == _plan.PersonDesignatedToMakeReservation.UserId));
            _harness.Core.Verify(c => c.SendMessage(It.Is<MessageToSend>(
                m => m.UserId == _plan.PersonDesignatedToMakeReservation.UserId
                     && m.Text.Contains("I need someone to help me make a reservation at a suitable location"))), Times.Once);

        }



    }
}