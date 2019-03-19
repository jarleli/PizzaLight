using System.Collections.Generic;

namespace PizzaLight.Infrastructure
{
    public interface IActivityLog
    {
        void Log(string activity);
        List<string> Activities { get; }
    }
}