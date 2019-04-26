using System.Collections.Generic;

namespace PizzaLight.Infrastructure
{
    public interface IActivityLog
    {
        void Log(string message);
        List<ApplicationActivity> Activities { get; }
    }
}