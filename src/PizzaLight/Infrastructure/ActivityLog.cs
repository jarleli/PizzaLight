using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Serilog;

namespace PizzaLight.Infrastructure
{
    public class ActivityLog : IActivityLog
    {
        private readonly ILogger _logger;
        private readonly List<ApplicationActivity> _activities;
        private static readonly object o = new object();

        public ActivityLog(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _activities = new List<ApplicationActivity>(100);
        }

        public List<ApplicationActivity>  Activities => _activities;

        /// <summary>
        /// Will log event to activity log. This will be written to the logger and be visible on the activity controller api
        /// Will throttle activity if more than 10 actions are logged per second to prevent spamming in case of a unhandled loops.
        /// </summary>
        /// <param name="message"></param>
        public void Log(string message)
        {
            _logger.Information(message);

            lock (o)
            {
                if (_activities.Count == 100)
                {
                    _activities.RemoveRange(0, 25);
                }
                var activity = new ApplicationActivity()
                {
                    Timestamp = DateTimeOffset.UtcNow,
                    Event = $"{DateTimeOffset.UtcNow.LocalDateTime} - {message}"
                };
                _activities.Add(activity);
                if (_activities.Count(a => a.Timestamp > activity.Timestamp.AddSeconds(-10)) > 10)
                {
                    _logger.Warning("Throttling application activity. Too many actions may be because of unintended loop.");
                    Thread.Sleep(1000);
                }
            }
        }
    }

    public class ApplicationActivity
    {
        public DateTimeOffset Timestamp { get; set; }
        public string Event { get; set; }
    }
}