using System.Collections.Generic;
using System.Linq;
using Noobot.Core.MessagingPipeline.Response;
using PizzaLight.Models;

namespace PizzaLight.Resources.ExtensionClasses
{
    public static class PizzaPlanMessageExtension
    {
        public static IEnumerable<ResponseMessage> CreateParticipantsLockedResponseMessage(this PizzaPlan pizzaPlan)
        {
            foreach (var person in pizzaPlan.Accepted)
            {
                var day = pizzaPlan.TimeOfEvent.LocalDateTime.ToString("dddd, MMMM dd");
                var time = pizzaPlan.TimeOfEvent.LocalDateTime.ToString("HH:mm");
                var participantlist = pizzaPlan.Accepted.GetStringListOfPeople("@");

                var text = $"*Great news!* \n" +
                           $"This amazing group of people has accepted the invitation for pizza on *{day} at {time}* \n" +
                           $"{participantlist} \n" +
                           $"If you don't know them all yet, now is an excellent opportunity. Please have a fantatic time!";

                var message = new ResponseMessage()
                {
                    ResponseType = ResponseType.DirectMessage,
                    UserId = person.UserId,
                    Text = text,
                };
                yield return message;
            }
        }
        public static ResponseMessage CreateNewDesignateToMakeReservationMessage(this PizzaPlan pizzaPlan)
        {
            var day = pizzaPlan.TimeOfEvent.LocalDateTime.ToString("dddd, MMMM dd");
            var time = pizzaPlan.TimeOfEvent.LocalDateTime.ToString("HH:mm");
            var participantlist = pizzaPlan.Accepted.GetStringListOfPeople("@");

            var text = $"Hello again, @{pizzaPlan.PersonDesignatedToMakeReservation.UserName} \n" +
                       $"I need someone to help me make a reservation at a suitable location for the upcoming pizza dinner planned on *{day} at {time}*.\n" +
                       $"I have chosen you for this honor and wish you the best of luck to find a suitable location and make the necessary arrangements. If you need help finding a venue or have any questions please head over to #pizzalight. \n" +
                       $"Someone else has been chosen to pay for the event and handling the expensing part, all you have to do is to make a reservation. \n" +
                       $"Also remember to inform or invite the other participants once you have made the reservation. The other participants are {participantlist} \n" +
                       $"Thank you!";
            return new ResponseMessage()
            {
                ResponseType = ResponseType.DirectMessage,
                UserId = pizzaPlan.PersonDesignatedToMakeReservation.UserId,
                Text = text
            };
        }

        public static ResponseMessage CreateNewDesignateToHandleExpensesMessage(this PizzaPlan pizzaPlan)
        {
            var day = pizzaPlan.TimeOfEvent.LocalDateTime.ToString("dddd, MMMM dd");
            var time = pizzaPlan.TimeOfEvent.LocalDateTime.ToString("HH:mm");
            var participantlist = pizzaPlan.Accepted.GetStringListOfPeople("@");

            var text = $"Hello again, @{pizzaPlan.PersonDesignatedToHandleExpenses.UserName} \n" +
                       $"I need someone to help me handle the expenses for the upcoming pizza dinner planned on *{day} at {time}*.\n" +
                       $"I have chosen you for this honor. What you have to do is pay the bill for the dinner and file for the expenses. Someone else will chose a venue and make a reservation, all you have to do is show up and be ready to pay. \n" +
                       $"If you have any questions please ask someone else in your group or head over to #pizzalight .\n" +
                       $"The other participants are {participantlist} \n" +
                       $"Thank you!";
            return new ResponseMessage()
            {
                ResponseType = ResponseType.DirectMessage,
                UserId = pizzaPlan.PersonDesignatedToHandleExpenses.UserId,
                Text = text
            };
        }

        public static IEnumerable<ResponseMessage> CreateNewEventIsCancelledMessage(this PizzaPlan pizzaPlan)
        {
            var day = pizzaPlan.TimeOfEvent.LocalDateTime.ToString("dddd, MMMM dd");
            var time = pizzaPlan.TimeOfEvent.LocalDateTime.ToString("HH:mm");

            foreach (var person in pizzaPlan.Accepted)
            {
                var text = $"Hello again @{person.UserName}. \n" +
                           $"Unfortunately due to lack of interest the *pizza dinner on {day} at {time} will have to be cancelled.* \n " +
                           $"I'll make sure to invite you to another dinner at another time.";
                yield return new ResponseMessage()
                {
                    ResponseType = ResponseType.DirectMessage,
                    UserId = person.UserId,
                    Text = text
                };
            }
        }

        public static  IEnumerable<ResponseMessage> CreateRemindParticipantsOfEvent(this PizzaPlan pizzaPlan)
        {
            var day = pizzaPlan.TimeOfEvent.LocalDateTime.ToString("dddd, MMMM dd");
            var time = pizzaPlan.TimeOfEvent.LocalDateTime.ToString("HH:mm");
            var participantlist = pizzaPlan.Accepted.GetStringListOfPeople("@");

            foreach (var person in pizzaPlan.Accepted)
            {
                var text = $"Hello again @{person.UserName}. \n" +
                           $"I'm sending you this message to remind you that you have an upcoming *pizza dinner on {day} at {time}* together with {participantlist} \n ";
                           
                yield return new ResponseMessage()
                {
                    ResponseType = ResponseType.DirectMessage,
                    UserId = person.UserId,
                    Text = text
                };
            }
        }


    }
}