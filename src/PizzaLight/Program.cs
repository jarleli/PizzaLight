using System;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;
using Serilog.Filters;

namespace PizzaLight
{
    class Program
    {
        public static void Main(string[] args)
        {
            string configFile = @"data/config/apitoken.json";
            if (args.Any())
            {
                configFile = args[0];
            }

            if (!File.Exists(configFile))
            {
                throw new InvalidOperationException(
                    $"No such config file {configFile}. Current working directory is {Directory.GetCurrentDirectory()}");
            }
            var stateFile = "data/state.json";
            if (!File.Exists(stateFile))
            {
                throw new InvalidOperationException(
                    $"No such config file {stateFile}. Current working directory is {Directory.GetCurrentDirectory()}");
            }

            var logger = ConfigureAndCreateSerilogLogger();
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddJsonFile(configFile)
                .AddCommandLine(args)
                .Build();

            var httpUri = "http://0.0.0.0:5000";
            logger.Information("Starting HTTP server on " + httpUri);

            try
            {
                var host = new WebHostBuilder()
                       .UseConfiguration(configuration)
                       .ConfigureLogging((context, builder) =>
                       {
                           builder.Services.AddSingleton<Serilog.ILogger>(logger);
                           builder.Services.AddSingleton<ILoggerFactory>(services => new SerilogLoggerFactory(logger, false));
                       })
                       .UseStartup<Startup>()
                       .UseKestrel()
                       .UseUrls(httpUri)
                       .Build();

                using (host)
                {
                    host.Start();
                    host.WaitForShutdown();
                }
            }
            catch (Exception e)
            {
                logger.Fatal(e, "Error starting app!");
                throw;
            }
        }

        public static Serilog.ILogger ConfigureAndCreateSerilogLogger()
        {
            var loggerConfiguration = new LoggerConfiguration()
                .WriteTo.Console();

            loggerConfiguration
                .WriteTo.File("data/logs/pizzabot_.log", rollOnFileSizeLimit : true, rollingInterval : RollingInterval.Day);

            loggerConfiguration.Enrich.FromLogContext();
            loggerConfiguration.Filter
                .ByExcluding(e=> Matching.FromSource("Microsoft.AspNetCore")(e) && e.Level < LogEventLevel.Warning);

            loggerConfiguration.MinimumLevel.Verbose();
            var logger = loggerConfiguration.CreateLogger();
            logger.Debug("Initilized logger.");
            return logger;
        }
    }
}
