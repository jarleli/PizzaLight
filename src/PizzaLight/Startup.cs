using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using PizzaLight.Infrastructure;
using PizzaLight.Resources;

namespace PizzaLight
{
    public class Startup
    {
        private Serilog.ILogger _logger;
        private IHostingEnvironment HostingEnvironment { get; }
        private IConfiguration Configuration { get; }

        public Startup(IHostingEnvironment env, IConfiguration config)
        {
            HostingEnvironment = env;
            Configuration = config;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc()
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_1)
                .AddJsonOptions(options=> options.SerializerSettings.Formatting = Formatting.Indented);
            services.AddSingleton(Configuration.GetSection("Bot").Get<BotConfig>());
            services.AddSingleton<IFileStorage, JsonStorage>();
            services.AddSingleton<IActivityLog, ActivityLog>();
            services.AddSingleton<PizzaPlanner>();
            services.AddSingleton<IPizzaInviter, PizzaInviter>();
            services.AddSingleton<IPizzaCore, PizzaCore>();
            services.AddSingleton<PizzaServiceHost>();

            var cts = new CancellationTokenSource();
            services.AddSingleton(cts);
        }

        public void Configure(IApplicationBuilder app, IApplicationLifetime applicationLifetime, IHostingEnvironment env)
        {
            _logger = app.ApplicationServices.GetService<Serilog.ILogger>();

            app.UseMvc();
            app.Run(async context =>
            {
                var message = "Invalid route: " + context.Request.Path;
                _logger.Information(message);
                context.Response.StatusCode = 404;
                await context.Response.WriteAsync(message);
            });

            var host = app.ApplicationServices.GetService<PizzaServiceHost>();
            applicationLifetime.ApplicationStarted.Register(async () => await host.Start());
            applicationLifetime.ApplicationStopping.Register(host.Stop);    
        }
    }
}