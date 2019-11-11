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
        private readonly ILogger _logger;
        private readonly IPizzaCore _pizzaCore;
        private List<IMustBeInitialized> _resources;
        private List<IMessageHandler> _handlers;

        public PizzaServiceHost(ILogger logger, CancellationTokenSource cts, IPizzaCore pizzaCore, IPizzaInviter inviter, PizzaPlanner planner, IOptOutHandler optOutHandler, IAnnouncementHandler annoucementHandler, IActivityLog activityLog)
        {
            _activityLog = activityLog ?? throw new ArgumentNullException(nameof(activityLog));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cts = cts ?? throw new ArgumentNullException(nameof(cts));
            _pizzaCore = pizzaCore ?? throw new ArgumentNullException(nameof(pizzaCore));

            _resources = new List<IMustBeInitialized>() { inviter, planner, optOutHandler, annoucementHandler };
            _handlers = new List<IMessageHandler>() { inviter, optOutHandler };
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
                var startTasks = _resources.Select(r => r.Start());
                Task.WaitAll(startTasks.ToArray());

                _pizzaCore.AddMessageHandlerToPipeline(_handlers.ToArray());
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