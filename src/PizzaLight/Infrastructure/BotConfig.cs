namespace PizzaLight.Infrastructure
{
    public class BotConfig
    {
        public string SlackApiKey { get; set; }
        public PizzaRoom PizzaRoom { get; set; }
        public int InvitesPerEvent { get; set; }
        public string BotRoom { get; set; }
    }

    public class PizzaRoom
    {
        public string Room { get; set; }
        public string City { get; set; }
    }
}