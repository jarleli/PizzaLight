using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PizzaLight.Infrastructure;
using PizzaLight.Resources;
using IConfiguration = Microsoft.Extensions.Configuration.IConfiguration;

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
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
            services.AddSingleton(Configuration.GetSection("Bot").Get<BotConfig>());
            services.AddSingleton<JsonStorage>();
            services.AddSingleton<PizzaPlanner>();
            services.AddSingleton<PizzaInviter>();
            services.AddSingleton<PizzaCore>();
            services.AddSingleton<PizzaServiceHost>();
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