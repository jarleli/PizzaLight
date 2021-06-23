using SlackAPI;
using System;
using System.Collections.Generic;
using System.Text;

namespace PizzaLight.Models.SlackModels
{
    /// <summary>
    /// WIll send to channel id if set, if it is not will look up based on: 
    /// 1: channel name, 2: userId
    /// </summary>
    public class MessageToSend
    {
        public string Text { get; set; }
        public string ChannelId { get; set; }
        public string ChannelName { get; set; }
        public string UserId { get; set; }
        //public ResponseType ResponseType { get; set; }
        public Attachment[] Attachments { get; set; }

    }
}
