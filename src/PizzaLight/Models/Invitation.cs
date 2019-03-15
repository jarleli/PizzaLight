using System;

namespace PizzaLight.Resources
{
    public class Invitation
    {
        public string EventId { get; set; }
        public DateTimeOffset EventTime { get; set; }
        public string UserName { get; set; }
        public string UserId { get; set; }
        public bool Invited { get; set; }
        public bool Accepted { get; set; }
    }
}