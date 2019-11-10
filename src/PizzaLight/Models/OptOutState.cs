using PizzaLight.Resources;
using System.Collections.Concurrent;

namespace PizzaLight.Models
{
    public class OptOutState
    {
        public ChannelState[] Channels { get; set; } = new ChannelState[0];
    }

    public class ChannelState
    {
        public string ChannelName { get; set; }
        public ConcurrentBag<Person> UsersThatHaveOptedOut { get; set; } = new ConcurrentBag<Person>();
    }

}