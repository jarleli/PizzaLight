using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using PizzaLight.Infrastructure;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;

namespace PizzaLight.Tests
{
    [TestFixture]
    public class ConfigFromJsonTests
    {
        [Test]
        public void CanReadConfigFromJson()
        {
            var config = GetConfig();
            Assert.That(config.InvitesPerEvent != 0);
            Assert.That(config.InvitesPerEvent is int);
        }

       
        [Test]
        public void PizzaRoomHasRoomAndCity()
        {
            var config = GetConfig();
            Assert.AreEqual("oslo" , config.PizzaRoom.Room);
            Assert.AreEqual("Oslo" , config.PizzaRoom.City);
        }

        private static BotConfig GetConfig()
        {
            var configuration = new ConfigurationBuilder()
                            .AddJsonFile("appsettings.json")
                            .Build();
            var config = configuration.GetSection("Bot").Get<BotConfig>();
            return config;
        }

    }
}
