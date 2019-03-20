using System;
using System.Collections.Generic;
using PizzaLight.Models;
using SlackConnector.Models;

namespace PizzaLight.Resources.ExtensionClasses
{
    public static class PersonExtensions
    {
        public static List<Person> SelectListOfRandomPeople(this List<SlackUser> inviteCandidates, int targetGuestCount)
        {
            var random = new Random();
            var numberOfCandidates = inviteCandidates.Count;
            var numberToInvite = Math.Min(targetGuestCount, numberOfCandidates);

            var list = new List<Person>(numberToInvite);
            for (int i = 0; i < numberToInvite; i++)
            {
                var randomPick = random.Next(0, numberOfCandidates);
                var invite = inviteCandidates[randomPick];
                inviteCandidates.Remove(invite);
                numberOfCandidates--;
                list.Add(new Person() { UserName = invite.Name, UserId = invite.Id });
            }
            return list;
        }

    }
}