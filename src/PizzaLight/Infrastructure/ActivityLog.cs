using System;
using System.Collections.Generic;
using Serilog;

namespace PizzaLight.Infrastructure
{
    public class ActivityLog : IActivityLog
    {
        private readonly ILogger _logger;
        private readonly List<string> _activities;
        private static readonly object o = new object();

        public ActivityLog(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _activities = new List<string>(100);
        }

        public List<string>  Activities => _activities;

        public void Log(string activity)
        {
            _logger.Information(activity);

            lock (o)
            {
                if (_activities.Count == 100)
                {
                    _activities.RemoveRange(0, 25);
                }
                _activities.Add($"{DateTimeOffset.UtcNow.LocalDateTime} - {activity}");
            }
        }
    }
}