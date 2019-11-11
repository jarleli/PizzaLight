using System;
using System.Collections.Generic;
using System.IO;
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
        private readonly IOptOutHandler _optOutHandler;
        private readonly ILogger _logger;
        private readonly IPizzaCore _pizzaCore;
        private List<IMustBeInitialized> _resources;

        public PizzaServiceHost(ILogger logger, CancellationTokenSource cts, IPizzaCore pizzaCore, IPizzaInviter inviter, PizzaPlanner planner, IOptOutHandler optOutHandler, IActivityLog activityLog)
        {
            _activityLog = activityLog ?? throw new ArgumentNullException(nameof(activityLog));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cts = cts ?? throw new ArgumentNullException(nameof(cts));
            _pizzaCore = pizzaCore ?? throw new ArgumentNullException(nameof(pizzaCore));
            _inviter = inviter ?? throw new ArgumentNullException(nameof(inviter));
            _planner = planner ?? throw new ArgumentNullException(nameof(planner));
            _optOutHandler = optOutHandler ?? throw new ArgumentNullException(nameof(optOutHandler));
        }

        public async Task Start()
        {
            try
            {
                await _pizzaCore.Start();
                if (!_pizzaCore.SlackConnection?.IsConnected ?? true)
                {
                    throw new OperationCanceledException("Could not connect to slack.");
                }
                _pizzaCore.AddMessageHandlerToPipeline(_inviter, _optOutHandler);
                _resources = new List<IMustBeInitialized>() {_inviter, _planner, _optOutHandler };
                var startTasks = _resources.Select(r => r.Start());
                Task.WaitAll(startTasks.ToArray());
                _activityLog.Log($"{this.GetType().Name} is up and running.");

            }
            catch (Exception e)
            {
                _logger.Fatal(e, "Error starting PizzaServiceHost.");
                throw;
            }
        }

        public void Stop()
        {
            _logger.Debug("Stopping all resources.");
            _cts.Cancel();
            var tasks = _resources.Select(r => r.Stop()).ToList();
            tasks.Add(_pizzaCore.Stop());

            Task.WaitAll(tasks.ToArray());
            _activityLog.Log($"{this.GetType().Name} has stopped all resources. Exiting.");

            _logger.Information("All resources stopped. Exiting.");
        }
    }
}