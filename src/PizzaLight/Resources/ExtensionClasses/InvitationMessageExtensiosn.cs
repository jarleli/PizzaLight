using PizzaLight.Models;
using PizzaLight.Models.SlackModels;
using SlackAPI.WebSocketMessages;

namespace PizzaLight.Resources.ExtensionClasses
{
    public static class InvitationMessageExtensiosn
    {
        public static MessageToSend CreateNewInvitationMessage(this Invitation invitation, string botRoom)
        {
            var day = invitation.EventTime.LocalDateTime.ToString("dddd, MMMM dd");
            var time = invitation.EventTime.LocalDateTime.ToString("HH:mm");
            var message = new MessageToSend()
            {
                Text =
                    $"Hello @{invitation.UserName} \n" +
                    $"Do you want to meet up for a social gathering and eat some tasty pizza with other colleagues in {invitation.City} on *{day} at {time}*? \n" +
                    $"Four other random colleagues from #{invitation.Room} have also been invited, and if you want to get to know them better all you have to do is reply yes if you want to accept this invitation or no if you can't make it and I will invite someone else in your stead. And don't worry, you will get a new chance in the future even if you can't make it this time.\n" +
                    $"If you have any questions please direct them to #{botRoom} and we will try to help. \n" +

                    "Please reply `yes` or `no`. Or if you rather I don't bother you again try typing `opt out`",

                UserId = invitation.UserId,
            };
            return message;
        }
        public static MessageToSend CreateReminderMessage(this Invitation reminder)
        {
            var day = reminder.EventTime.LocalDateTime.ToString("dddd, MMMM dd");
            var time = reminder.EventTime.LocalDateTime.ToString("HH:mm");
            var message = new MessageToSend()
            {
                Text =
                    $"Hello @{reminder.UserName} \n" +
                    $"I recently sent you an invitation for a social pizza event on *{day} at {time}*. \n" +
                    "Since you haven't responded yet I'm sending you this friendly reminder. If you don't respond promptly I will assume that you cannot make it and will invite someone else instead. \n" +
                    "Please reply `yes` or `no` to indicate whether you can make it..",

                UserId = reminder.UserId,
            };
            return message;
        }

        public static MessageToSend CreateExpiredInvitationMessage(this Invitation invitation)
        {
            var message = new MessageToSend()
            {
                Text =
                    $"Hello @{invitation.UserName} \n" +
                    $"Sadly, you didn't respond to my invitation and I will now invite someone else instead. \n" +
                    $"Don't worry, I will try again sometime later. Maybe we will have better luck then.",
                UserId = invitation.UserId,
            };
            return message;
        }

        public static MessageToSend UserTurnsDownInvitation(this NewMessage incoming)
        {
            return incoming.CreateResponseMessage("That is too bad, I will try to find someone else. \n" +
                "If you don't want to receive any more invitations from me try typing `opt out`");
        }
    }
}