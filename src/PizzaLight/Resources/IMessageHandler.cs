using System.Threading.Tasks;
using Noobot.Core.MessagingPipeline.Request;

namespace PizzaLight.Resources
{
    public interface IMessageHandler
    {
        Task HandleMessage(IncomingMessage incomingMessage);
    }
}