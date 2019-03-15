using System;
using System.Diagnostics;
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
                var configuration = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json")
                    .AddJsonFile(@"C:\temp\slack-temp\apitoken.json")
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
