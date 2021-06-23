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
    public class GetsAcceptedMessageForInvitation
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
                new NewMessage() {user = _plan.Invited.First().UserId, text= "yes"});
        }

        [Test]
        public void UserIsListedAsAccepted()
        {
            Assert.IsNotNull(_plan.Accepted.SingleOrDefault(p => p.UserId == _userId));

        }

        [Test]
        public void ReplyToUserWeGotHisAnswer()
        {
            _harness.Core.Verify(c => c.SendMessage(
                It.Is<MessageToSend>(m => m.Text.Contains(
                    "Thank you. I will keep you informed when the other guests have accepted!"))));
        }
    }
}