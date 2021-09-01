using SlackAPI;

namespace PizzaLight.Resources
{
    [RequestPath("conversations.members")]
    public class ConversationMembers : Response
    {
        public string[] members { get; set; }
    }

}