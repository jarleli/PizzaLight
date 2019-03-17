using System;
using System.Collections.Generic;

namespace PizzaLight.Models
{
    public class PizzaPlan
    {
        public string Id { get; set; }
        public DateTimeOffset TimeOfEvent { get; set; }
        public List<Person> Invited { get; set; } = new List<Person>();
        public List<Person> Rejected { get; set; } = new List<Person>();
        public List<Person> Accepted { get; set; } = new List<Person>();
        public bool ParticipantsLocked { get; set; }
    }
    public class Person
    {
        public string UserName { get; set; }
        public string UserId { get; set; }
    }
}