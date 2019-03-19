using System.Collections.Generic;
using System.Threading.Tasks;
using Noobot.Core.MessagingPipeline.Response;
using PizzaLight.Infrastructure;
using SlackConnector;
using SlackConnector.Models;

namespace PizzaLight.Resources
{
    public interface IPizzaCore
    {
        ISlackConnection SlackConnection { get; }
        IReadOnlyDictionary<string, SlackUser> UserCache { get; }
        Task MessageReceived(SlackMessage message);
        Task SendMessage(ResponseMessage responseMessage);
        Task Start();
        void Stop();
        void AddMessageHandlerToPipeline(IMessageHandler inviter);
    }
}