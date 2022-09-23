using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using PizzaLight.Infrastructure;
using PizzaLight.Models;
using PizzaLight.Models.SlackModels;
using PizzaLight.Resources;
using PizzaLight.Tests.Harness;

namespace PizzaLight.Tests.Unit
{
    [TestFixture, Category("Unit")]
    public class PizzaPlannerUnitTests
    {
        private TestHarness _harness;
        private BotConfig _config;
        
        [SetUp]
        public void Setup()
        {
            _harness = TestHarness.CreateHarness();
            _config = new BotConfig { NorwegianHolidays = "1-1;1-5;17-5;22-12;23-12;24-12;25-12;26-12;27-12;28-12;29-12;30-12;31-12" };
            _harness.Start();
        }

        [Test]
        public async Task GetPeopleToInviteReturnsRightNumberOfPeople()
        {
            var cache = _harness.UserCache;
            var result = await _harness.Planner.FindPeopleToInvite(_harness.Config.PizzaRoom.Room, 2, new List<Person>());
            Assert.That(result.Count == 2,"result.Count == 2");
            Assert.That(cache.Any(u=>u.name == result[0].UserName));
            Assert.That(cache.Any(u=>u.name == result[1].UserName));
            Assert.That( result[0].UserName != result[1].UserName);
        }

        [Test]
        public void GetDayOfNextEvent_ReturnsSomeWeekDayInTheFuture()
        {
            var dateTime =  _harness.Planner.GetTimeOfNextEvent(_harness.FuncNow().AddDays(7).Date);
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
            
            _harness.Storage.Verify(s=>s.SaveArray(PizzaPlanner.ACTIVEEVENTSFILE, It.IsAny<PizzaPlan[]>()), Times.Once);
            _harness.Storage.Verify(s=>s.SaveArray(PizzaPlanner.OLDEVENTSFILE, It.IsAny<PizzaPlan[]>()), Times.Once);
            _harness.Activity.Verify(s=>s.Log(It.IsAny<string>()), Times.Once);

            Assert.That(!_harness.ActivePizzaPlans.Any());
            Assert.That(_harness.OldPizzaPlans.Length == 1);
        }

        [Test]
        public void ScheduledOnANotPublicHoliday()
        {
            const string notHoliday = "23-09-2022";
            var aHoliday = DateTime.ParseExact(notHoliday, "dd-MM-yyyy", CultureInfo.CurrentCulture);

            var retVal = PizzaPlanner.IsScheduledDateIsAHoliday(aHoliday, _config.NorwegianHolidays);

            Assert.AreEqual(false, retVal);
        }

        [Test]
        public void ScheduledOnAPublicHoliday()
        {
            const string holiday = "17-05-2022";
            var aHoliday = DateTime.ParseExact(holiday, "dd-MM-yyyy", CultureInfo.CurrentCulture);

            var retVal = PizzaPlanner.IsScheduledDateIsAHoliday(aHoliday, _config.NorwegianHolidays);

            Assert.AreEqual(true, retVal);
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

            _harness.Storage.Verify(s => s.SaveArray(PizzaPlanner.ACTIVEEVENTSFILE, It.IsAny<PizzaPlan[]>()), Times.Once);
            _harness.Storage.Verify(s => s.SaveArray(PizzaPlanner.OLDEVENTSFILE, It.IsAny<PizzaPlan[]>()), Times.Never);
            _harness.Core.Verify(s => s.SendMessage(It.IsAny<MessageToSend>()), Times.Exactly(4));
            _harness.Activity.Verify(s => s.Log(It.IsAny<string>()), Times.Once);

            Assert.That(_harness.ActivePizzaPlans.Length == 1);
            Assert.That(_harness.ActivePizzaPlans.Single().ParticipantsLocked);
            Assert.That(_harness.OldPizzaPlans.Length == 0);
        }
    }
}
