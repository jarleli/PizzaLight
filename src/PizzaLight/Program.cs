using System;
using System.Diagnostics;
using System.IO;
using Common.Logging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.AspNetCore;

namespace PizzaLight
{
    class Program
    {
        public static void Main(string[] args)
        {
            try
            {
#if (DEBUG)
                string configFile = @"C:\temp\slack-temp\config\apitoken.json";
#elif (RELEASE)
                string configFile = @"data/config/apitoken.json";
#endif

                if (!File.Exists(configFile))
                {
                    throw new InvalidOperationException($"No such config file {configFile}. Current working directory is {Directory.GetCurrentDirectory()}");
                }
                var stateMarker = "data/state.json";
                if (!File.Exists(stateMarker))
                {
                    throw new InvalidOperationException($"No such config file {stateMarker}. Current working directory is {Directory.GetCurrentDirectory()}");
                }



                var configuration = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json")
                    .AddJsonFile(configFile)
                    .AddCommandLine(args)
                    .Build();

                var httpUri = "http://0.0.0.0:5000";
                var host = new WebHostBuilder()
                    .UseConfiguration(configuration)
                    .ConfigureLogging((context, builder) =>
                    {
                        var logger = new LoggerConfiguration().WriteTo.ColoredConsole().CreateLogger();
                        builder.Services.AddSingleton<Serilog.ILogger>(logger);
                        builder.Services.AddSingleton<ILoggerFactory>(services => new SerilogLoggerFactory(logger, false));
                    } )
                    .UseStartup<Startup>()
                    .UseKestrel()
                    .UseUrls(httpUri)
                    .Build();

                Console.WriteLine("Starting HTTP server on " + httpUri);
                using (host)
                {
                    host.Start();
                    host.WaitForShutdown();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e);
                Console.ReadLine();
            }
        }
    }
}
