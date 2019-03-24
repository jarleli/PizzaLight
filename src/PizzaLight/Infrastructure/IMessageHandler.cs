using System.Threading.Tasks;
using Noobot.Core.MessagingPipeline.Request;

namespace PizzaLight.Infrastructure
{
    public interface IMessageHandler
    {
        Task<bool> HandleMessage(IncomingMessage incomingMessage);
    }
}