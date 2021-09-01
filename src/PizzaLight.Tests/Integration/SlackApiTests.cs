using Microsoft.Extensions.Configuration;
using Moq;
using NUnit.Framework;
using PizzaLight.Infrastructure;
using PizzaLight.Resources;
using System.Linq;
using System.Threading.Tasks;

namespace PizzaLight.Tests
{
    [TestFixture, Category("Integration")]
    public class SlackApiTests
    {
        [Test]
        public async Task CanOpenConversation()
        {
            var config = ConfigFromJsonTests.GetConfig();
            var core = new PizzaCore(config, new Mock<Serilog.ILogger>().Object);
            await core.Start();
            var users = core.UserCache;
            var user = users.First(u => u.name == "jarlelin");

            var res = await core.OpenUserConversation(user.id);
        }


        [Test]
        public async Task CheckChannelMembers()
        {
            var config = ConfigFromJsonTests.GetConfig();
            var core = new PizzaCore(config, new Mock<Serilog.ILogger>().Object);
            await core.Start();
            var channel = core.Channels.Single(c => c.name == config.PizzaRoom.Room);

            var members = await core.ChannelMembers(config.PizzaRoom.Room);
            Assert.IsNotEmpty(members);
        }

        [Test]
        public async Task CanSendHello()
        {
            var config = ConfigFromJsonTests.GetConfig();
            var core = new PizzaCore(config, new Mock<Serilog.ILogger>().Object);
            await core.Start();
            var users = core.UserCache;
            var user = users.First(u => u.name == "jarlelin");

            await core.SendMessage(new Models.SlackModels.MessageToSend { UserId = user.id, Text = "test hi" });
        }

        [Test]
        public async Task SendHelloToUserChannel()
        {
            var config = ConfigFromJsonTests.GetConfig();
            var core = new PizzaCore(config, new Mock<Serilog.ILogger>().Object);
            await core.Start();
            var users = core.UserCache;
            var user = users.First(u => u.name == "jarlelin");
            
            var chan = await core.OpenUserConversation(user.id);

            await core.SendMessage(new Models.SlackModels.MessageToSend { ChannelId= chan.id, UserId = user.id, Text = "check my username" });
        }




    }
}
