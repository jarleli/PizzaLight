using System;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PizzaLight.Infrastructure;
using PizzaLight.Resources;

namespace PizzaLight
{
    public class Startup
    {
        private Serilog.ILogger _logger;
        private IConfiguration Configuration { get; }

        public Startup(IConfiguration config)
        {
            Configuration = config;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddSingleton(Configuration.GetSection("Bot").Get<BotConfig>());
            services.AddSingleton<IFileStorage, JsonStorage>();
            services.AddSingleton<OptOutState, OptOutState>();
            services.AddSingleton<IOptOutHandler, OptOutHandler>();
            services.AddSingleton<IOptOutState, OptOutState>();
            services.AddSingleton<IActivityLog, ActivityLog>();
            services.AddSingleton<IAnnouncementHandler, AnnouncementHandler>();
            services.AddSingleton<IPizzaInviter, PizzaInviter>();
            services.AddSingleton<IPizzaCore, PizzaCore>();
            services.AddSingleton<PizzaPlanner>();
            services.AddSingleton(()=> DateTimeOffset.UtcNow);
            services.AddHostedService<PizzaServiceHost>();

            var cts = new CancellationTokenSource();
            services.AddSingleton(cts);
        }

        public void Configure(IApplicationBuilder app)
        {
            _logger = app.ApplicationServices.GetService<Serilog.ILogger>();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            app.Run(async context =>
            {
                var message = "Invalid route: " + context.Request.Path;
                _logger.Information(message);
                context.Response.StatusCode = 404;
                await context.Response.WriteAsync(message);
            });
        }
    }
}