using System.Threading.Tasks;
using SlackAPI.WebSocketMessages;

namespace PizzaLight.Infrastructure
{
    public interface IMessageHandler
    {
        Task<bool> HandleMessage(NewMessage incomingMessage);
    }
}