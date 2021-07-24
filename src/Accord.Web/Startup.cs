using Accord.Bot;
using Accord.Bot.Infrastructure;
using Accord.Domain;
using Accord.Services;
using Accord.Services.Raid;
using Accord.Web.Infrastructure.DiscordOAuth;
using MediatR;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
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
            services.AddRazorPages();
            services.AddServerSideBlazor();

            services.AddAuthentication(opt =>
                {
                    opt.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    opt.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    opt.DefaultChallengeScheme = DiscordOAuthConstants.AUTHENTICATION_SCHEME;
                })
                .AddCookie()
                .AddDiscord(x =>
                {
                    x.ClientId = _configuration["DiscordConfiguration:ClientId"];
                    x.ClientSecret = _configuration["DiscordConfiguration:ClientSecret"];
                    x.SaveTokens = true;
                });

            services
                .AddDbContext<AccordContext>(x => x.UseSqlServer(_configuration.GetConnectionString("Database")))
                .AddLazyCache()
                .AddMediatR(typeof(ServiceResponse).Assembly, typeof(BotClient).Assembly)
                .AddDiscordBot(_configuration)
                .AddSingleton<RaidCalculator>()
                .AddSingleton<IEventQueue, EventQueue>();

            // Configure hosted services
            //services
            //    .AddHostedService<BotHostedService>()
            //    .AddHostedService<EventQueueProcessor>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment environment)
        {
            if (environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting(); 
            app.UseAuthentication();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapBlazorHub();
                endpoints.MapFallbackToPage("/_Host");
            });
        }
    }
}