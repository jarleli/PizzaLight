using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;

namespace PizzaLight.Resources
{
    public class OptOutState : IOptOutState
    {
        public ConcurrentDictionary<string, ChannelState> ChannelList { get; set; }
        public OptOutState()
        {
            ChannelList = new ConcurrentDictionary<string, ChannelState>();

        }
        public Task AddUserToOptOutOfChannel(string userId, string channel)
        {
            throw new NotImplementedException();
        }

        public Task RemoveUserFromOptOutOfChannel(string userId, string channel)
        {
            throw new NotImplementedException();
        }
    }

    public class ChannelState
    {
        public string ChannelName { get; set; }
        public ConcurrentBag<string> UsersThatHaveOptedOut { get; set; } = new ConcurrentBag<string>();
    }

    public interface IOptOutState
    {
        ConcurrentDictionary<string, ChannelState> ChannelList { get;}
        Task AddUserToOptOutOfChannel(string userId, string channel);
        Task RemoveUserFromOptOutOfChannel(string userId, string channel);

    }
}
