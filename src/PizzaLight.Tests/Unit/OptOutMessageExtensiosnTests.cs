using System;
using System.Collections.Generic;
using System.Linq;
using Noobot.Core.MessagingPipeline.Request;
using NUnit.Framework;
using PizzaLight.Infrastructure;
using PizzaLight.Models;
using PizzaLight.Resources.ExtensionClasses;

namespace PizzaLight.Tests.Unit
{
    [TestFixture, Category("Unit")]
    public class OptOutMessageExtensiosnTests
    {
        private BotConfig _config;
        private IncomingMessage _message;

        [OneTimeSetUp]
        public void Setup()
        {
            _config = new BotConfig() { PizzaRoom = new PizzaRoom { Room = "testroom" } };
            _message = new IncomingMessage();
        }

        [Test]
        public void ListOfRooms()
        {
            var reply = _message.ProvideChannelOptionToOptOut(_config);
            StringAssert.Contains("where options for Channel include: `testroom` or `all`", reply.Text);
        }
    }
}