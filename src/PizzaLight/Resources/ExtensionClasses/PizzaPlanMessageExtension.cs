﻿using System.Collections.Generic;
using System.Linq;
using PizzaLight.Models;
using PizzaLight.Models.SlackModels;

namespace PizzaLight.Resources.ExtensionClasses
{
    public static class PizzaPlanMessageExtension
    {
        public static IEnumerable<MessageToSend> CreateParticipantsLockedResponseMessage(this PizzaPlan pizzaPlan)
        {
            foreach (var person in pizzaPlan.Accepted)
            {
                var day = pizzaPlan.TimeOfEvent.LocalDateTime.ToString("dddd, MMMM dd");
                var time = pizzaPlan.TimeOfEvent.LocalDateTime.ToString("HH:mm");
                var participantlist = pizzaPlan.Accepted.GetListOfFormattedUserId();

                var text = $"*Great news!* \n" +
                           $"This amazing group of people has accepted the invitation for pizza on *{day} at {time}* \n" +
                           $"{participantlist} \n" +
                           $"If you don't know them all yet, now is an excellent opportunity. Please be mindful of local recommendations, stay safe and have a fantastic time!";

                var message = new MessageToSend()
                {
                    UserId = person.UserId,
                    Text = text,
                };
                yield return message;
            }
        }

        public static MessageToSend CreatePizzaRoomAnnouncementMessage(this PizzaPlan pizzaPlan, string botRoom)
        {
            var day = pizzaPlan.TimeOfEvent.LocalDateTime.ToString("dddd, MMMM dd");
            var time = pizzaPlan.TimeOfEvent.LocalDateTime.ToString("HH:mm");
            var participantlist = pizzaPlan.Accepted.GetListOfFormattedUserId();

            var text = $"On *{day} at {time}*, {participantlist} will get together and eat some tasty pizza.\n" +
                       $"{pizzaPlan.PersonDesignatedToMakeReservation.GetFormattedUserId()} will make a reservation for the group.\n" +
                       $"{pizzaPlan.PersonDesignatedToHandleExpenses.GetFormattedUserId()} will expense the meal afterwards.\n" +
                       $"All the rest of you have to do is show up and have a great time and get to know each other better.\n" +
                       $"Maybe once you are done you can upload an image of you all enjoying yourselves?" +
                       $"" +
                       $"I want to remind you all to be mindful of local recommendations during these times and please stay at home if you are showing any symptoms.";

            var message = new MessageToSend()
            {
                ChannelName = botRoom,
                Text = text
            };

            
            return message;
        }

        public static MessageToSend CreateNewDesignateToMakeReservationMessage(this PizzaPlan pizzaPlan, string botRoom)
        {
            var day = pizzaPlan.TimeOfEvent.LocalDateTime.ToString("dddd, MMMM dd");
            var time = pizzaPlan.TimeOfEvent.LocalDateTime.ToString("HH:mm");
            var participantlist = pizzaPlan.Accepted.GetListOfFormattedUserId();

            var text = $"Hello again, @{pizzaPlan.PersonDesignatedToMakeReservation.UserName} \n" +
                       $"I need someone to help me make a reservation at a suitable location for the upcoming pizza dinner planned on *{day} at {time}*.\n" +
                       $"I have chosen you for this honor and wish you the best of luck to find a suitable location and make the necessary arrangements. If you need help finding a venue or have any questions please head over to #{botRoom}. \n" +
                       $"Someone else has been chosen to pay for the event and expense it, all you have to do is to make a reservation. \n" +
                       $"Also remember to inform or invite the other participants once you have made the reservation. The other participants are {participantlist} \n" +
                       $"Thank you!";
            return new MessageToSend()
            {
                UserId = pizzaPlan.PersonDesignatedToMakeReservation.UserId,
                Text = text
            };
        }

        public static MessageToSend CreateNewDesignateToHandleExpensesMessage(this PizzaPlan pizzaPlan, string botRoom)
        {
            var day = pizzaPlan.TimeOfEvent.LocalDateTime.ToString("dddd, MMMM dd");
            var time = pizzaPlan.TimeOfEvent.LocalDateTime.ToString("HH:mm");
            var participantlist = pizzaPlan.Accepted.GetListOfFormattedUserId();

            var text = $"Hello again, @{pizzaPlan.PersonDesignatedToHandleExpenses.UserName} \n" +
                       $"I need someone to help me handle the expenses for the upcoming pizza dinner planned on *{day} at {time}*.\n" +
                       $"I have chosen you for this honor. What you have to do is pay the bill for the dinner and file for the expenses. Someone else will chose a venue and make a reservation, all you have to do is show up and be ready to pay. \n" +
                       $"If you have any questions please ask someone else in your group or head over to #{botRoom} .\n" +
                       $"The participants are {participantlist} \n" +
                       $"Thank you!";
            return new MessageToSend()
            {
                UserId = pizzaPlan.PersonDesignatedToHandleExpenses.UserId,
                Text = text
            };
        }

        public static IEnumerable<MessageToSend> CreateNewEventIsCancelledMessage(this PizzaPlan pizzaPlan)
        {
            var day = pizzaPlan.TimeOfEvent.LocalDateTime.ToString("dddd, MMMM dd");
            var time = pizzaPlan.TimeOfEvent.LocalDateTime.ToString("HH:mm");

            foreach (var person in pizzaPlan.Accepted)
            {
                var text = $"Hello again @{person.UserName}. \n" +
                           $"Unfortunately due to lack of interest the *pizza dinner on {day} at {time} will have to be cancelled.* \n " +
                           $"I'll make sure to invite you to another dinner at another time.";
                yield return new MessageToSend()
                {
                    UserId = person.UserId,
                    Text = text
                };
            }
        }

        public static  IEnumerable<MessageToSend> CreateRemindParticipantsOfEvent(this PizzaPlan pizzaPlan)
        {
            var day = pizzaPlan.TimeOfEvent.LocalDateTime.ToString("dddd, MMMM dd");
            var time = pizzaPlan.TimeOfEvent.LocalDateTime.ToString("HH:mm");
            var participantlist = pizzaPlan.Accepted.GetListOfFormattedUserId();

            foreach (var person in pizzaPlan.Accepted)
            {
                var text = $"Hello again @{person.UserName}. \n" +
                           $"I'm sending you this message to remind you that you have an upcoming *pizza dinner on {day} at {time}* together with {participantlist} \n ";
                           
                yield return new MessageToSend()
                {
                    UserId = person.UserId,
                    Text = text
                };
            }
        }


    }
}