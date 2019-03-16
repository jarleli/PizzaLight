using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace PizzaLight.Models
{
    public class Invitation
    {
        public string EventId { get; set; }
        public DateTimeOffset EventTime { get; set; }
        public string UserName { get; set; }
        public string UserId { get; set; }
        public bool Invited { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public Invitation.ResponseEnum Response { get; set; }

        public enum ResponseEnum
        {
            NoResponse,
            Accepted,
            Rejected
        }
    }
}