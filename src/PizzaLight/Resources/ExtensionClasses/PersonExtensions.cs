﻿using System;
using System.Collections.Generic;
using System.Linq;
using PizzaLight.Models;
using SlackAPI;
using SlackAPI.WebSocketMessages;

namespace PizzaLight.Resources.ExtensionClasses
{
    public static class PersonExtensions
    {
        public static List<Person> SelectListOfRandomPeople(this List<User> inviteCandidates, int targetGuestCount)
        {
            var numberOfCandidates = inviteCandidates.Count;
            var numberToInvite = Math.Min(targetGuestCount, numberOfCandidates);

            var list = new List<Person>(numberToInvite);
            for (int i = 0; i < numberToInvite; i++)
            {
                var randomPick = Random.Shared.Next(0, numberOfCandidates);
                var invite = inviteCandidates[randomPick];
                inviteCandidates.Remove(invite);
                numberOfCandidates--;
                list.Add(new Person() { UserName = invite.name, UserId = invite.id });
            }
            return list;
        }

        public static string GetStringListOfPeople(this List<Person> input, string prefixNames = "")
        {
            if (input.Count == 0) return "";
            if(input.Count == 1) return $"{prefixNames}{input.First().UserName}";
            if(input.Count == 2) return $"{prefixNames}{input[0].UserName} and {prefixNames}{input[1].UserName}";

            if (input.Count > 2)
            {
                var firstArray = input.GetRange(0, input.Count-1);
                var part1 = string.Join(", ", firstArray.Select(a => $"{prefixNames}{a.UserName}"));
                return $"{part1} and {prefixNames}{input.Last().UserName}";
            }
            throw new Exception("");
        }

        public static string GetListOfFormattedUserId(this List<Person> input)
        {
            if (input.Count == 0) return "";
            if (input.Count == 1) return $"{input.First().GetFormattedUserId()}";
            if (input.Count == 2) return $"{input[0].GetFormattedUserId()} and {input[1].GetFormattedUserId()}";

            if (input.Count > 2)
            {
                var firstArray = input.GetRange(0, input.Count - 1);
                var part1 = string.Join(", ", firstArray.Select(a => $"{a.GetFormattedUserId()}"));
                return $"{part1} and {input.Last().GetFormattedUserId()}";
            }
            throw new Exception("");
        }

        public static Person GetSendingUser(this NewMessage incomingMessage)
        {
            return new Person { UserId = incomingMessage.user, UserName = incomingMessage.username};
        }


    }
}