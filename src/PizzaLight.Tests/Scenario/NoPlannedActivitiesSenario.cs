using System;
using System.Collections.Generic;
using System.Linq;
using Moq;
using NUnit.Framework;
using PizzaLight.Models;
using PizzaLight.Resources;
using PizzaLight.Tests.Harness;
using PizzaLight.Tests.Unit;
using Serilog.Core;

namespace PizzaLight.Tests.Scenario
{
    [TestFixture, Category("Unit")]
    public class NoPlannedActivitiesSenario
    {
        private TestHarness _harness;

        [OneTimeSetUp]
        public void SetUp()
        {
            _harness = TestHarness.CreateHarness();
            
            _harness.Planner.Start().Wait();
            _harness.Inviter.Start().Wait();


            _harness.Planner.PizzaPlannerScheduler().Wait();
            _harness.Logger.Verify(l=>l.Fatal(It.IsAny<Exception>(), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void ANewActivityIsPlannedForNextWeek()
        {
            _harness.Storage.Verify(i=>i.SaveFile(PizzaPlanner.ACTIVEEVENTSFILE,It.Is<PizzaPlan[]>(l=>l.Count() == 1)));
        }

        [Test]
        public void InvitationsWereSentToNewActivity()
        {
            _harness.Storage.Verify(i => i.SaveFile(PizzaInviter.INVITESFILE, It.Is<Invitation[]>(l => l.Count() == _harness.Config.InvitesPerEvent)));
        }
    }
}