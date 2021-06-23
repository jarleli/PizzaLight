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
    public class InvitedGuestTurnsDownInvitation
    {
        private TestHarness _harness;
        private PizzaPlan _plan;
        private string _userId;

        [OneTimeSetUp]
        public async Task SetUp()
        {
            _harness = TestHarness.CreateHarness();
            _harness.Start();

            _harness.Tick();
            _plan = _harness.ActivePizzaPlans.Single();
            _userId = _plan.Invited.First().UserId;
            await _harness.Inviter.HandleMessage(
                new NewMessage() {user = _plan.Invited.First().UserId, text= "no"});
        }

        [Test]
        public void UserIsListedAsRejcted()
        {
            Assert.IsNotNull(_plan.Rejected.SingleOrDefault(p => p.UserId == _userId));
            Assert.IsNull(_plan.Invited.SingleOrDefault(p => p.UserId == _userId));
            Assert.AreEqual(4, _plan.Invited.Count);
        }

        [Test]
        public void ReplyToUserWeGotHisAnswer()
        {
            _harness.Core.Verify(c => c.SendMessage(
                It.Is<MessageToSend>(m => m.Text.Contains(
                    "That is too bad, I will try to find someone else."))));
        }
    }
}