using System;
using System.Linq;
using Moq;
using Noobot.Core.MessagingPipeline.Response;
using NUnit.Framework;
using PizzaLight.Models;
using PizzaLight.Resources;
using PizzaLight.Tests.Harness;

namespace PizzaLight.Tests.Scenario.Planner
{
    [TestFixture, Category("Unit")]
    public class PlansANewActivityEveryWeek
    {
        private TestHarness _harness;

        [OneTimeSetUp]
        public void SetUp()
        {
            _harness = TestHarness.CreateHarness();

            _harness.Start();

            _harness.Tick();
            _harness.Now = _harness.Now.AddDays(4);
            _harness.Tick();

            _harness.Logger.Verify(l => l.Fatal(It.IsAny<Exception>(), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void ANewActivityIsPlannedForNextWeek()
        {
            Assert.AreEqual(2 , _harness.ActivePizzaPlans.Length);
            Assert.AreEqual(0 , _harness.OldPizzaPlans.Length);
        }

        [Test]
        public void ThereareInvitationsForEachPizzaPlan()
        {
            Assert.AreEqual(10, _harness.InvitationList.Length);
            var planIds = _harness.ActivePizzaPlans.Select(p => p.Id);
            foreach (var pId in planIds)
            {
                Assert.AreEqual(_harness.Config.InvitesPerEvent, _harness.InvitationList.Count(i=>i.EventId == pId));
            }
        }

        [Test]
        public void UsersWereMessagedForEeachInvitation()
        {
            Assert.AreEqual(10, _harness.InvitationList.Length);
            VerifyInvitationMessagesSent();
        }

        private void VerifyInvitationMessagesSent()
        {
            foreach (var invitation in _harness.InvitationList)
            {
                _harness.Core.Verify(c => c.SendMessage(It.Is<ResponseMessage>(m
                    =>
                        m.UserId == invitation.UserId
                        && m.Text.Contains(
                            "Do you want to meet up for a social gathering and eat some tasty pizza with other colleagues in")
                )), Times.Once);
            }
        }
    }
}