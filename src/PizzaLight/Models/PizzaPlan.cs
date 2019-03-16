using System;
using System.Collections.Generic;

namespace PizzaLight.Resources
{
    public class PizzaPlan
    {
        public string Id { get; set; }
        public DateTimeOffset TimeOfEvent { get; set; }
        public List<Person> Invited { get; set; } = new List<Person>();
        public List<Person> Rejected { get; set; } = new List<Person>();
        public List<Person> Accepted { get; set; } = new List<Person>();
        public bool ParticipatntsLocked { get; set; }
    }
    public class Person
    {
        //public string EventId { get; set; }
        public string UserName { get; set; }
        public string UserId { get; set; }
        //public  bool Invited { get; set; }
        //public  bool Accepted { get; set; }
    }
}