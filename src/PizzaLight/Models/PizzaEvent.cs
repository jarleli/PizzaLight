using System;

namespace PizzaLight.Resources
{
    public class PizzaEvent
    {
        public string Id { get; set; }
        public DateTimeOffset TimeOfEvent { get; set; }
        public Invite[] Invited { get; set; }

     

    }
    public class Invite
    {
        //public string EventId { get; set; }
        public string UserName { get; set; }
        public string UserId { get; set; }
        //public  bool Invited { get; set; }
        //public  bool Accepted { get; set; }
    }
}