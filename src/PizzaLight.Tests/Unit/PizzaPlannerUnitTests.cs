using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Flurl.Util;
using Moq;
using Noobot.Core.MessagingPipeline.Response;
using NUnit.Framework;
using PizzaLight.Models;
using PizzaLight.Resources;
using PizzaLight.Tests.Harness;

namespace PizzaLight.Tests.Unit
{
    [TestFixture]
    public class PizzaPlannerUnitTests
    {
        private TestHarness _harness;

        [SetUp]
        public void Setup()
        {
            _harness = TestHarness.CreateHarness();
            //await _harness.Planner.Start();
            //await _harness.Inviter.Start();
        }

        [Test]
        public void GetPeopleToInviteReturnsRightNumberOfPeople()
        {
            var cache = _harness.UserCache;
            var result = _harness.Planner.FindPeopleToInvite(_harness.Config.PizzaRoom.Room, 2, new List<Person>());
            Assert.That(result.Count == 2,"result.Count == 2");
            Assert.That(cache.Values.Any(u=>u.Name == result[0].UserName));
            Assert.That(cache.Values.Any(u=>u.Name == result[1].UserName));
            Assert.That( result[0].UserName != result[1].UserName);
        }

        [Test]
        public void GetDayOfNextEvent_ReturnsSomeWeekDayInTheFuture()
        {
            var dateTime = _harness.Planner.GetTimeOfNextEvent(_harness.FuncNow().AddDays(7).Date);
            Assert.That(dateTime.DayOfWeek != DayOfWeek.Saturday);
            Assert.That(dateTime.DayOfWeek != DayOfWeek.Sunday);
            Assert.That(dateTime> _harness.FuncNow());
            Assert.That(dateTime.TimeOfDay>TimeSpan.FromHours(15));
        }

        [Test]
        public async Task UpcomingEventWithTooFewGuestsIsCancelled()
        {
            _harness.HasUpcomingPizzaPlans(new[]
            {
                new PizzaPlan()
                {
                    Id = Bip39Words.WordGenerator.GetRandomWordString(3),
                    TimeOfEvent = _harness.FuncNow().AddDays(3)
                }
            });

            await _harness.Planner.Start();
            await _harness.Planner.LockInPizzaPlansOrCancelOnesThatPassDeadline();


            //performs only one operation to change the plan
            
            _harness.Storage.Verify(s=>s.SaveFile(PizzaPlanner.ACTIVEEVENTSFILE, It.IsAny<PizzaPlan[]>()), Times.Once);
            _harness.Storage.Verify(s=>s.SaveFile(PizzaPlanner.OLDEVENTSFILE, It.IsAny<PizzaPlan[]>()), Times.Once);
            _harness.Activity.Verify(s=>s.Log(It.IsAny<string>()), Times.Once);

            Assert.That(!_harness.ActivePizzaPlans.Any());
            Assert.That(_harness.OldPizzaPlans.Length == 1);
        }

        [Test]
        public async Task UpcomingEventWithEnoughGuestsIsLockedIn()
        {
            _harness.HasUpcomingPizzaPlans(new[]
            {
                new PizzaPlan()
                {
                    Id = Bip39Words.WordGenerator.GetRandomWordString(3),
                    TimeOfEvent = _harness.FuncNow().AddDays(3),
                    Accepted = new  List<Person>(4)
                    {
                        new Person(){UserId = "a"},
                        new Person(){UserId = "b"},
                        new Person(){UserId = "c"},
                        new Person(){UserId = "d"},
                    },
                    ParticipantsLocked = false
                }
            });

            await _harness.Planner.Start();
            _harness.Planner.LockInPizzaPlansOrCancelOnesThatPassDeadline().Wait();

            _harness.Storage.Verify(s => s.SaveFile(PizzaPlanner.ACTIVEEVENTSFILE, It.IsAny<PizzaPlan[]>()), Times.Once);
            _harness.Storage.Verify(s => s.SaveFile(PizzaPlanner.OLDEVENTSFILE, It.IsAny<PizzaPlan[]>()), Times.Never);
            _harness.Core.Verify(s => s.SendMessage(It.IsAny<ResponseMessage>()), Times.Exactly(4));
            _harness.Activity.Verify(s => s.Log(It.IsAny<string>()), Times.Once);

            Assert.That(_harness.ActivePizzaPlans.Length == 1);
            Assert.That(_harness.ActivePizzaPlans.Single().ParticipantsLocked);
            Assert.That(_harness.OldPizzaPlans.Length == 0);
        }
    }
}
