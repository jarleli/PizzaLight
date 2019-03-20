using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PizzaLight.Infrastructure;
using PizzaLight.Resources;
using Serilog;

namespace PizzaLight
{
    public class PizzaServiceHost
    {
        private readonly IActivityLog _activityLog;
        private readonly CancellationTokenSource _cts;
        private readonly IPizzaInviter _inviter;
        private readonly PizzaPlanner _planner;
        private readonly ILogger _logger;
        private readonly IPizzaCore _pizzaCore;
        private List<IMustBeInitialized> _resources;

        public PizzaServiceHost(ILogger logger, CancellationTokenSource cts, IPizzaCore pizzaCore, IPizzaInviter inviter, PizzaPlanner planner, IActivityLog activityLog)
        {
            _activityLog = activityLog ?? throw new ArgumentNullException(nameof(activityLog));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cts = cts ?? throw new ArgumentNullException(nameof(cts));
            _pizzaCore = pizzaCore ?? throw new ArgumentNullException(nameof(pizzaCore));
            _inviter = inviter ?? throw new ArgumentNullException(nameof(inviter));
            _planner = planner ?? throw new ArgumentNullException(nameof(planner));
        }

        public async Task Start()
        {
            await _pizzaCore.Start();
            _pizzaCore.AddMessageHandlerToPipeline(_inviter);

            _resources = new List<IMustBeInitialized>(){ _inviter, _planner};
            var startTasks = _resources.Select(r => r.Start());
            Task.WaitAll(startTasks.ToArray());
            _activityLog.Log($"{this.GetType().Name} is up and running.");
        }

        public void Stop()
        {
            _logger.Debug("Stopping all resources.");
            _cts.Cancel();
            _pizzaCore.Stop();
            var startTasks = _resources.Select(r => r.Stop());
            Task.WaitAll(startTasks.ToArray());
            _activityLog.Log($"{this.GetType().Name} has stopped all resources. Exiting.");

            _logger.Information("All resources stopped. Exiting.");
        }
    }
}