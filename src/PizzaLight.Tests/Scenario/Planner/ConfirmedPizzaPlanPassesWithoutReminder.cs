using System;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using PizzaLight.Models;
using PizzaLight.Resources;
using PizzaLight.Tests.Harness;

namespace PizzaLight.Tests.Scenario.Planner
{
    [TestFixture, Category("Unit")]
    public class ConfirmedPizzaPlanPassesWithoutReminder
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
                await _harness.Inviter.HandleMessage(new SlackAPI.WebSocketMessages.NewMessage() { user = person.UserId, text= "yes" });
            }
            _harness.Tick();
            _harness.Core.Invocations.Clear();
            _harness.Now = _plan.TimeOfEvent.AddHours(6);
            _harness.Tick();

            _harness.Logger.Verify(l => l.Fatal(It.IsAny<Exception>(), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void ArchivesFinishedPizzaPlan()
        {
            var archivedPlan = _harness.OldPizzaPlans.Single(p=>p.Id == _plan.Id);
            Assert.IsTrue(archivedPlan.ParticipantsLocked);
            Assert.IsTrue(archivedPlan.FinishedSuccessfully);
        }

        [Test]
        public void SkipsSendingLateReminder()
        {
            var archivedPlan = _harness.OldPizzaPlans.Single(p => p.Id == _plan.Id);
            Assert.IsNull(archivedPlan.SentReminder);
        }


    }
}