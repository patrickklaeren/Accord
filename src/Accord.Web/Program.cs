using System.Linq;
using Accord.Bot;
using Accord.Bot.Infrastructure;
using Accord.Domain;
using Accord.Services;
using Accord.Services.Raid;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sentry;
using Serilog;
using Serilog.Events;

Log.Information("Starting up...");

var builder = WebApplication
    .CreateBuilder(args);

builder.Logging.AddSerilog(CreateLogger());

builder.Services
    .AddDbContext<AccordContext>(x => x.UseSqlServer(builder.Configuration.GetConnectionString("Database")))
    .AddLazyCache()
    .AddHttpClient()
    .AddMediatR(typeof(ServiceResponse).Assembly, typeof(BotClient).Assembly)
    .AddDiscordBot(builder.Configuration)
    .AddSingleton<RaidCalculator>()
    .AddSingleton<IEventQueue, EventQueue>();

// Configure hosted services
builder.Services
    .AddHostedService<BotHostedService>()
    .AddHostedService<EventQueueProcessor>();

var app = builder.Build();

Log.Information("Host built...");

using (var scope = app.Services.CreateScope())
{
    Log.Information("Scope created...");

    var services = scope.ServiceProvider;

    await using var db = services.GetRequiredService<AccordContext>();

    Log.Information("Got Db context");

    await db.Database.MigrateAsync();

    Log.Information("Migrated!");
}

Log.Information("Ready to run");

app.MapGet("/", () => "Hello world!");

await app.RunAsync();

ILogger CreateLogger()
{
    var loggerConfiguration = new LoggerConfiguration();
    
    loggerConfiguration.MinimumLevel.Debug()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Debug)
        .Enrich.FromLogContext();

    if (builder.Environment.IsDevelopment())
    {
        loggerConfiguration.WriteTo.Console();
    }

    var sentrySection = builder.Configuration.GetSection("SentryConfiguration");

    if (!string.IsNullOrWhiteSpace(sentrySection["Dsn"]))
    {
        loggerConfiguration.WriteTo.Sentry(o =>
        {
            o.MinimumBreadcrumbLevel = LogEventLevel.Information;
            o.MinimumEventLevel = LogEventLevel.Warning;
            o.Dsn = sentrySection["Dsn"];
            o.Environment = sentrySection["Environment"];
            o.BeforeSend = BeforeSend;
        });
    }

    return loggerConfiguration.CreateLogger();
}

SentryEvent? BeforeSend(SentryEvent arg)
{
    var ignoredLogMessages = new[]
    {
        "No matching command could be found.",
        "Guild User requesting the command does not have the required Administrator permission",
        "Unknown interaction"
    };

    if (arg.Message?.Formatted is not null
        && ignoredLogMessages.Any(x => arg.Message.Formatted.Contains(x)))
    {
        return null;
    }

    var hasCode = arg.Extra.TryGetValue("StatusCode", out var code);

    // Don't log 404's
    if (hasCode && code?.ToString() == "404")
        return null;

    return arg;
}