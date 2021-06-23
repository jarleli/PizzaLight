using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using PizzaLight.Infrastructure;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;

namespace PizzaLight.Tests
{
    [TestFixture, Category("Integration")]
    public class ConfigFromJsonTests
    {
        [Test]
        public void CanReadConfigFromJson()
        {
            var config = GetConfig();
            Assert.That(config.InvitesPerEvent != 0);
        }

       
        [Test]
        public void PizzaRoomHasRoomAndCity()
        {
            var config = GetConfig();
            Assert.AreEqual("bot-team", config.PizzaRoom.Room);
            Assert.AreEqual("Oslo", config.PizzaRoom.City);
        }

        public static BotConfig GetConfig()
        {
            var configuration = new ConfigurationBuilder()
                            .AddJsonFile("appsettings.json")
                            .AddJsonFile(@"c:\temp\pizzalight\data\config\apitoken.json")
                            .Build();
            var config = configuration.GetSection("Bot").Get<BotConfig>();
            Assert.Greater(config.SlackApiKey.Length, 8);
            return config;
        }

    }
}
