using System;
using System.Collections.Generic;
using Noobot.Core.MessagingPipeline.Response;

namespace PizzaLight.Models
{
    public class PizzaPlan
    {
        public string Id { get; set; }
        public string Channel { get; set; }
        public string City { get;  set; }
        public DateTimeOffset TimeOfEvent { get; set; }
        public List<Person> Invited { get; set; } = new List<Person>();
        public List<Person> Accepted { get; set; } = new List<Person>();
        public List<Person> Rejected { get; set; } = new List<Person>();
        public bool ParticipantsLocked { get; set; }
        public Person PersonDesignatedToMakeReservation { get; set; }
        public Person PersonDesignatedToHandleExpenses { get; set; }
        public DateTimeOffset? SentReminder { get; set; }
        public bool FinishedSuccessfully { get; set; }
        public DateTimeOffset? Cancelled { get; set; }
    }
    public class Person
    {
        public string UserName { get; set; }
        public string UserId { get; set; }
    }
}