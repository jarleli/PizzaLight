using Noobot.Core.Configuration;

namespace PizzaLight.Infrastructure
{
    public class BotConfig

    {
        public string SlackApiKey { get; set; }

        //public bool HelpEnabled { get; set; }
        //public bool StatsEnabled { get; }
        //public bool AboutEnabled { get; }

        public string[] Channels { get; set; }
        public string RoomToInviteFrom { get; set; }
        public int InvitesPerEvent { get; set; }
    }
}