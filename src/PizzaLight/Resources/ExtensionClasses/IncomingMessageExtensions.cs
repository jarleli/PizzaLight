using System;
using System.Linq;
using Noobot.Core.MessagingPipeline.Request;
using Noobot.Core.MessagingPipeline.Response;
using PizzaLight.Models;

namespace PizzaLight.Resources.ExtensionClasses
{
    public static class MessageExtensions
    {
        public static string GetTargetedText(this IncomingMessage incomingMessage)
        {
            var messageText = incomingMessage.FullText ?? string.Empty;
            var isOnPrivateChannel = incomingMessage.ChannelType == ResponseType.DirectMessage;

            string[] myNames =
            {
                    incomingMessage.BotName + ":",
                    incomingMessage.BotName,
                    $"<@{incomingMessage.BotId}>:",
                    $"<@{incomingMessage.BotId}>",
                    $"@{incomingMessage.BotName}:",
                    $"@{incomingMessage.BotName}",
                };

            var handle = myNames.FirstOrDefault(x => messageText.StartsWith(x, StringComparison.OrdinalIgnoreCase));
            if (string.IsNullOrEmpty(handle) && !isOnPrivateChannel)
            {
                return string.Empty;
            }

            if (string.IsNullOrEmpty(handle) && isOnPrivateChannel)
            {
                return messageText;
            }

            return messageText.Substring(handle.Length).Trim();
        }

    }
}