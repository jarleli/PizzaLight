using PizzaLight.Models.SlackModels;
using SlackAPI;
using SlackAPI.WebSocketMessages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PizzaLight.Resources.ExtensionClasses
{
    public static class NewMessageExtensions
    {
        
        public static bool IsByBot(this NewMessage message)
        {
            return !string.IsNullOrWhiteSpace(message.bot_id);
        }
        public static bool IsDirect(this NewMessage message)
        {
            return (message.channel[0] == 'D');
        }

        public static bool IsGroup(this NewMessage message)
        {
            return (message.channel[0] == 'G');
        }

        public static bool IsChannel(this NewMessage message)
        {
            return (message.channel[0] == 'C');
        }

        public static MessageToSend CreateResponseMessage(this NewMessage input, string text)
        {
            return new MessageToSend()
            {
                ChannelId = input.channel,
                UserId = input.user,
                Text = text
            };
        }

        public static string GetTargetedText(this NewMessage input, Self self)
        {
            var messageText = input.text ?? string.Empty;
            var isOnPrivateChannel = input.IsDirect() || input.IsGroup();

            string[] myNames =
            {
                    self.name + ":",
                    self.name,
                    $"<@{self.id}>:",
                    $"<@{self.id}>",
                    $"@{self.name}:",
                    $"@{self.name}",
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
