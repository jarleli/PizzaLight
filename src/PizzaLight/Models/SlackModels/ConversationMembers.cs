using SlackAPI;

namespace PizzaLight.Resources
{
    [RequestPath("conversations.members")]
    public class ConversationMembers : Response
    {
        public string[] members { get; set; }
        public CursorMetadata response_metadata { get; set; }
    }

    public class CursorMetadata
    {
        public string next_cursor { get; set; }
    }
}