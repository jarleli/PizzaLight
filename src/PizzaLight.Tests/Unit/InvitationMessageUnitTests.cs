using PizzaLight.Models;
using System;
using System.Collections.Generic;
using System.Text;
using PizzaLight.Resources.ExtensionClasses;
using NUnit.Framework;

namespace PizzaLight.Tests.Unit
{
    [TestFixture, Category("Unit")]
    public class MessageTests
    {
        [Test]
        public void InvitationUsesCityInInvitationText()
        {
            var invitation = new Invitation() {UserName="testuser",City="testcity", Room="testroom" };
            var message = invitation.CreateNewInvitationMessage("botroom");


            StringAssert.Contains("colleagues in " + invitation.City, message.Text);
            StringAssert.Contains("colleagues from #" + invitation.Room, message.Text);

            Console.WriteLine(message.Text);
        }

        [Test]
        public void ReservationMessageTest()
        {
            var pizzaplan = CreatePizzaPlan();
            var message = pizzaplan.CreateNewDesignateToMakeReservationMessage("botroom");

            Console.WriteLine(message.Text);
            StringAssert.Contains(pizzaplan.PersonDesignatedToMakeReservation.UserName , message.Text);
        }

        [Test]
        public void ExpensesMessageTest()
        {
            var pizzaplan = CreatePizzaPlan();
            var message = pizzaplan.CreateNewDesignateToHandleExpensesMessage("botroom");

            Console.WriteLine(message.Text);
            StringAssert.Contains(pizzaplan.PersonDesignatedToHandleExpenses.UserName, message.Text);
        }

        private PizzaPlan CreatePizzaPlan()
        {
            return  new PizzaPlan() { TimeOfEvent = DateTimeOffset.Now, PersonDesignatedToMakeReservation = new Person { UserName = "user1" }, PersonDesignatedToHandleExpenses = new Person { UserName = "user2" } };
        }
    }
}
