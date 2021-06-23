using System;
using System.Linq;
using Moq;
using NUnit.Framework;
using PizzaLight.Models;
using PizzaLight.Models.SlackModels;
using PizzaLight.Resources;
using PizzaLight.Tests.Harness;

namespace PizzaLight.Tests.Scenario.Planner
{
    [TestFixture, Category("Unit")]
    public class MakesNewPizzaPlanIfNoneIsPlanned
    {
        private TestHarness _harness;

        [OneTimeSetUp]
        public void SetUp()
        {
            _harness = TestHarness.CreateHarness();
            _harness.Start();


            _harness.Tick();
            _harness.Logger.Verify(l=>l.Fatal(It.IsAny<Exception>(), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void ANewActivityIsPlannedForNextWeek()
        {
            _harness.Storage.Verify(i=>i.SaveArray(PizzaPlanner.ACTIVEEVENTSFILE,It.Is<PizzaPlan[]>(l=>l.Count() == 1)));
            Assert.AreEqual(1, _harness.ActivePizzaPlans.Length);
        }

        [Test]
        public void InvitationsWereSentToPotentialGuestsForNewPizzaPlan()
        {
            _harness.Storage.Verify(i => i.SaveArray(PizzaInviter.INVITESFILE, It.Is<Invitation[]>(l => l.Count() == _harness.Config.InvitesPerEvent)));
            Assert.AreEqual(5, _harness.InvitationList.Length);

            _harness.Core.Verify(c => c.SendMessage(
                    It.Is<MessageToSend>(m 
                        => m.Text.Contains("Do you want to meet up for a social gathering and eat some tasty pizza ")
                        && _harness.InvitationList.Any(i=>i.UserId == m.UserId)
                    )),Times.Exactly(5));


        }
    }
}