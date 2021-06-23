using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using PizzaLight.Models;
using PizzaLight.Models.SlackModels;
using PizzaLight.Tests.Harness;
using SlackAPI.WebSocketMessages;

namespace PizzaLight.Tests.Scenario.Inviter
{
    [TestFixture, Category("Unit")]
    public class InvitedGuestTurnsDownInvitation_WeInviteSomeoneElse
    {
        private TestHarness _harness;
        private PizzaPlan _plan;
        private string _userIdOfGustRejecting;
        private IEnumerable<string> _firstInvitedUsers;


        [OneTimeSetUp]
        public async Task SetUp()
        {
            _harness = TestHarness.CreateHarness();
            _harness.Start();

            _harness.Tick();
            _plan = _harness.ActivePizzaPlans.Single();
            _userIdOfGustRejecting = _plan.Invited.First().UserId;
            _firstInvitedUsers = _plan.Invited.Select(u => u.UserId).ToList();
            await _harness.Inviter.HandleMessage(
                new NewMessage() {user = _userIdOfGustRejecting, text= "no"});

            _harness.Core.Invocations.Clear();
            _harness.Tick();
        }

        [Test]
        public void InviteListHasNewGuestInsteadOfOld()
        {
            Assert.AreEqual(5, _plan.Invited.Count);
            Assert.IsNotNull(_plan.Rejected.SingleOrDefault(p => p.UserId == _userIdOfGustRejecting));
            Assert.IsNull(_plan.Invited.SingleOrDefault(p => p.UserId == _userIdOfGustRejecting));
        }


        [Test]
        public void SentNewInviteToNewUser()
        {
            var repeat = _plan.Invited.Where(i => _firstInvitedUsers.Any(f=>f==i.UserId));
            Assert.AreEqual(4, repeat.Count());
            var _newGuest = _plan.Invited.Single(i => !_firstInvitedUsers.Any(f=>f==i.UserId));

            _harness.Core.Verify(c => c.SendMessage(
                It.Is<MessageToSend>(m 
                => m.Text.Contains(
                    "Do you want to meet up for a social gathering and eat some")
                    && m.UserId == _newGuest.UserId)));
        }
    }
}