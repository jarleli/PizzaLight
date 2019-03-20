using System.Linq;
using Noobot.Core.MessagingPipeline.Response;
using PizzaLight.Models;

namespace PizzaLight.Resources.ExtensionClasses
{
    public static class PizzaPlanMessageExtension
    {
        public static ResponseMessage CreateParticipantsLockedResponseMessage(this PizzaPlan pizzaPlan, string participantlist,
            Person person)
        {
            var day = pizzaPlan.TimeOfEvent.LocalDateTime.ToString("dddd, MMMM dd");
            var time = pizzaPlan.TimeOfEvent.LocalDateTime.ToString("HH:mm");
            var text = $"Great news! This amazing group has accepted the invitation for a pizza date on *{day} at {time}* \n" +
                       $"{participantlist} \n" +
                       $"If you don't know them all yet, now is an excellent opportunity. Please have a fantatic time!";

            var message = new ResponseMessage()
            {
                ResponseType = ResponseType.DirectMessage,
                UserId = person.UserId,
                Text = text,
            };
            return message;
        }

        public static ResponseMessage CreateNewDesignateToMakeReservationMessage(this PizzaPlan pizzaPlan)
        {
            var day = pizzaPlan.TimeOfEvent.LocalDateTime.ToString("dddd, MMMM dd");
            var time = pizzaPlan.TimeOfEvent.LocalDateTime.ToString("HH:mm");
            var participantlist = string.Join(", ", pizzaPlan.Accepted.Select(a => $"@{a.UserName}"));

            var text = $"Hello again, @{pizzaPlan.PersonDesignatedToMakeReservation.UserName} \n" +
                       $"I need someone to help me make a reservation at a suitable location for the upcoming pizza date planned on *{day} at {time}*.\n" +
                       $"I have chosen you for this honor this time and wish you the best of luck to find a suitable location and make the necessary arrangements. If you have any questsions please ask someone else in your group or head over to #pizzalight. \n" +
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
            var participantlist = string.Join(", ", pizzaPlan.Accepted.Select(a => $"@{a.UserName}"));

            var text = $"Hello again, @{pizzaPlan.PersonDesignatedToHandleExpenses.UserName} \n" +
                       $"I need someone to help me handle the expenses for the upcoming pizza date planned on *{day} at {time}*.\n" +
                       $"I have chosen you for this honor this time. What you have to do is pay the bill for the dinner and file for the expenses.\n" +
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

    }
}