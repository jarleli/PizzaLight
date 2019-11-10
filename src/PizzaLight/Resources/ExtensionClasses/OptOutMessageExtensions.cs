using Noobot.Core.MessagingPipeline.Request;
using Noobot.Core.MessagingPipeline.Response;
using PizzaLight.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PizzaLight.Resources.ExtensionClasses
{
    public static class OptOutMessageExtensions
    {
        public static ResponseMessage ProvideChannelOptionToOptOut(this IncomingMessage incomingMessage, BotConfig config)
        {
            var channels = new[] { config.PizzaRoom.Room }.Select(s=>$"`{s}`").ToArray();
            var channelList = string.Join(", ", channels) + " or `all`";

            var message = incomingMessage.ReplyDirectlyToUser(
                            $"To opt out of receiving invitations from me, plese specify which channel you want to me ignore you in. \n" +
                            $"Use `opt out [Channel]`, " +
                            $"where options for Channel include: {channelList}");
            return message;
        }

        public static ResponseMessage RepeatOptOutMessageToConfirm(this IncomingMessage incomingMessage, string channel)
        {
            var message = incomingMessage.ReplyDirectlyToUser($"Please confirm your choice by repeating `opt out {channel}`");
            return message;
        }

        public static ResponseMessage ConfirmOptOutMessage(this IncomingMessage incomingMessage, string channel)
        {
            var message = incomingMessage.ReplyDirectlyToUser($"You have now opted out of any and all upcoming pizza plans in the channel '{channel}'.\n" +
                $"If you should change your mind you can allways opt in again by typing `opt in {channel}`.");
            return message;
        }

        public static ResponseMessage OptedIntoChannelAgain(this IncomingMessage incomingMessage, string channel)
        {
            var message = incomingMessage.ReplyDirectlyToUser($"You have now opted in for any upcoming pizza plans in the channel `{channel}`.\n" +
                $"I am really happy you have chosen to do so. Perhaps you will be selected soon to eat some delicious pizza.");
            return message;
        }

        public static ResponseMessage ChannelUnrecogised(this IncomingMessage incomingMessage, string channel)
        {
            var message = incomingMessage.ReplyDirectlyToUser($"I don't recognise that channel name: {channel}");
            return message;
        }
    }
}
