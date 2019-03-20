using Noobot.Core.MessagingPipeline.Response;
using PizzaLight.Models;

namespace PizzaLight.Resources.ExtensionClasses
{
    public static class InvitationMessageExtensiosn
    {
        public static ResponseMessage CreateNewInvitationMessage(this Invitation invitation)
        {
            var day = invitation.EventTime.LocalDateTime.ToString("dddd, MMMM dd");
            var time = invitation.EventTime.LocalDateTime.ToString("HH:mm");
            var message = new ResponseMessage()
            {
                Text =
                    $"Hello @{invitation.UserName}. \n" +
                    $"Do you want to meet up for a social gathering and eat some tasty pizza with other colleagues on *{day} at {time}*? \n" +
                    "Four other random colleagues have also been invited, and if you want to get to know them better all you have to do is reply yes if you want to accept this invitation or no if you can't make it and I will invite someone else in your stead. \n" +
                    "Please reply `yes` or `no`.",

                ResponseType = ResponseType.DirectMessage,
                UserId = invitation.UserId,
            };
            return message;
        }
        public static ResponseMessage CreateReminderMessage(this Invitation reminder)
        {
            var day = reminder.EventTime.LocalDateTime.ToString("dddd, MMMM dd");
            var time = reminder.EventTime.LocalDateTime.ToString("HH:mm");
            var message = new ResponseMessage()
            {
                Text =
                    $"Hello @{reminder.UserName}. I recently sent you an invitation for a social pizza event on *{day} at {time}*. \n" +
                    "Since you haven't responded yet I'm sending you this friendly reminder. If you don't respond before tomorrow I will assume that you cannot make it and will invite someone else instead. \n" +
                    "Please reply `yes` or `no` to indicate whether you can make it..",

                ResponseType = ResponseType.DirectMessage,
                UserId = reminder.UserId,
            };
            return message;
        }

        public static ResponseMessage CreateExpiredInvitationMessage(this Invitation invitation)
        {
            var message = new ResponseMessage()
            {
                Text =
                    $"Hello @{invitation.UserName}." +
                    $"Sadly, you didn't respond to my invitation and I will now invite someone else instead." +
                    $"Maybe we will have better luck sometime later.",

                ResponseType = ResponseType.DirectMessage,
                UserId = invitation.UserId,
            };
            return message;
        }
    }
}