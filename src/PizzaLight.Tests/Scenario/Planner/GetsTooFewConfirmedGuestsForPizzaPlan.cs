﻿using System;
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
    public class GetsTooFewConfirmedGuestsForPizzaPlan
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
            await _harness.Inviter.HandleMessage(new NewMessage() { user = _plan.Invited.First().UserId, text= "yes" });

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

        [Test]
        public void GuestsThatAcceptedAreNotifiedOfCancellation()
        {
            _harness.Core.Verify(c
                    => c.SendMessage(It.Is<MessageToSend>(m
                        => m.Text.Contains("Unfortunately due to lack of interest the *pizza dinner on")))
                , Times.Exactly(_plan.Accepted.Count));

        }


    }
}