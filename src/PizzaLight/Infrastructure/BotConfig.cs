
namespace PizzaLight.Infrastructure
{
    public class BotConfig

    {
        public string SlackApiKey { get; set; }

        //public bool HelpEnabled { get; set; }
        //public bool StatsEnabled { get; }
        //public bool AboutEnabled { get; }

        public PizzaRoom PizzaRoom { get; set; }
        public int InvitesPerEvent { get; set; }

        
    }
    public class PizzaRoom
    {
        public string Room { get; set; }
        public string City { get; set; }
    }
}