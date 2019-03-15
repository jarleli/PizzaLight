﻿using System;
using System.Linq;
using Noobot.Core.MessagingPipeline.Request;
using Noobot.Core.MessagingPipeline.Response;

namespace PizzaLight.NoobotInternals
{
        public static class IncomingMessageExtensions
        {
            public static string GetTargetedText(this IncomingMessage incomingMessage)
            {
                string messageText = incomingMessage.FullText ?? string.Empty;
                bool isOnPrivateChannel = incomingMessage.ChannelType == ResponseType.DirectMessage;

                string[] myNames =
                {
                    incomingMessage.BotName + ":",
                    incomingMessage.BotName,
                    $"<@{incomingMessage.BotId}>:",
                    $"<@{incomingMessage.BotId}>",
                    $"@{incomingMessage.BotName}:",
                    $"@{incomingMessage.BotName}",
                };

                string handle = myNames.FirstOrDefault(x => messageText.StartsWith(x, StringComparison.OrdinalIgnoreCase));
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