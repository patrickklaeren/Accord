using Accord.Bot;
using Accord.Bot.Infrastructure;
using Accord.Domain;
using Accord.Services;
using Accord.Services.ChannelFlags;
using Accord.Services.Permissions;
using Accord.Services.Raid;
using Accord.Services.Xp;
using Accord.Web.Hosted;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Accord.Web
{
    public class Startup
    {
        private readonly IConfiguration _configuration;

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddDbContext<AccordContext>(x => x.UseSqlServer(_configuration.GetConnectionString("Database")))
                .AddLazyCache()
                .AddMediatR(typeof(ServiceResponse).Assembly, typeof(BotClient).Assembly)
                .AddDiscordBot(_configuration)
                .AddSingleton<RaidCalculator>()
                .AddSingleton<IEventQueue, EventQueue>();

            // Configure hosted services
            services
                .AddHostedService<BotHostedService>()
                .AddHostedService<EventQueueProcessor>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/", async context =>
                {
                    await context.Response.WriteAsync("Hello World!");
                });
            });
        }
    }
}
