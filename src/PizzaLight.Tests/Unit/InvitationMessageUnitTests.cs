using PizzaLight.Models;
using System;
using System.Collections.Generic;
using System.Text;
using PizzaLight.Resources.ExtensionClasses;
using NUnit.Framework;

namespace PizzaLight.Tests.Unit
{
    [TestFixture]
    public class InvitationMessageUnitTests
    {
        [Test]
        public void InvitationUsesCityInInvitationText()
        {
            var invitation = new Invitation() {UserName="testuser",City="testcity", Room="testroom" };
            var message = invitation.CreateNewInvitationMessage();


            StringAssert.Contains("colleagues in " + invitation.City, message.Text);
            StringAssert.Contains("colleagues from " + invitation.Room, message.Text);

            Console.WriteLine(message.Text);
        }
    }
}
