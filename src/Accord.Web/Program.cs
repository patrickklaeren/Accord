using System.Threading.Tasks;
using Accord.Domain;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

namespace Accord.Web
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.File("logs\\log.txt")
                .CreateLogger();

            Log.Information("Starting up...");

            var host = CreateHostBuilder(args).Build();

            Log.Information("Host built...");

            using (var scope = host.Services.CreateScope())
            {
                Log.Information("Scope created...");

                var services = scope.ServiceProvider;

                await using var db = services.GetRequiredService<AccordContext>();

                Log.Information("Got Db context");

                await db.Database.MigrateAsync();

                Log.Information("Migrated!");
            }

            Log.Information("Ready to run");

            await host.RunAsync();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseSerilog()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
