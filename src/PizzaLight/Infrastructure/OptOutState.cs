using PizzaLight.Infrastructure;
using PizzaLight.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PizzaLight.Resources
{
    public class OptOutState : IOptOutState
    {
        private readonly IFileStorage _storage;

        const string PIZZABOTSTATEFILE = "optouts";
        public ConcurrentDictionary<string, ChannelState> ChannelList { get; set; }
        private readonly object _stateLock = new object();
        public OptOutState(IFileStorage storage)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        }
        public Task AddUserToOptOutOfChannel(Person user, string channelName)
        {
            lock (_stateLock)
            {
                if (!ChannelList.ContainsKey(channelName))
                {
                    ChannelList[channelName] = new ChannelState { ChannelName = channelName };
                }

                var channel = ChannelList[channelName];
                if(channel.UsersThatHaveOptedOut.Any(u=>u.UserId == user.UserId))
                {
                }
                else
                {
                    channel.UsersThatHaveOptedOut.Add(user);
                }
                SaveState();
                return Task.CompletedTask;
            }
        }
        
        public Task RemoveUserFromOptOutOfChannel(Person user, string channelName)
        {
            lock (_stateLock)
            {
                var channel = ChannelList[channelName];
                if (channel.UsersThatHaveOptedOut.Any(u => u.UserId == user.UserId))
                {
                    var list = channel.UsersThatHaveOptedOut.Where(u => u.UserId != user.UserId);
                    channel.UsersThatHaveOptedOut = new ConcurrentBag<Person>(list);
                    SaveState();
                }
                return Task.CompletedTask;
            }
        }

        public ChannelState GetStateForChannel(string channelName)
        {
            return ChannelList.ContainsKey(channelName) ? ChannelList[channelName] : null;
        }

        private void SaveState()
        {
            var state = _storage.ReadObject<Models.OptOutState>(PIZZABOTSTATEFILE) ?? new Models.OptOutState(); 
            state.Channels = ChannelList.Values.ToArray();
            _storage.SaveObject<Models.OptOutState>(PIZZABOTSTATEFILE, state);
        }

        public async Task Start()
        {
            await _storage.Start();

            lock (_stateLock)
            {
                var state = _storage.ReadObject<Models.OptOutState>(PIZZABOTSTATEFILE) ?? new Models.OptOutState();

                var dictionary = state.Channels.ToList().ToDictionary(c => c.ChannelName, c => c);

                ChannelList = new ConcurrentDictionary<string, ChannelState>(dictionary);
            }
        }

        public Task Stop()
        {
            _storage.Stop();

            return Task.CompletedTask;

        }
    }

    public interface IOptOutState : IMustBeInitialized
    {
        ConcurrentDictionary<string, ChannelState> ChannelList { get; set; }
        Task AddUserToOptOutOfChannel(Person user, string channelName);
        Task RemoveUserFromOptOutOfChannel(Person user, string channelName);
        ChannelState GetStateForChannel(string channelName);

    }
}
