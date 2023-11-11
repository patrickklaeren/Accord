using System.Linq;
using System.Security.Claims;
using Accord.Bot;
using Accord.Bot.HostedServices;
using Accord.Bot.Infrastructure;
using Accord.Domain;
using Accord.Services;
using AspNet.Security.OAuth.Discord;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MudBlazor.Services;
using Sentry;
using Serilog;
using Serilog.Events;

Log.Information("Starting up...");

var builder = WebApplication
    .CreateBuilder(args);

builder.Logging.AddSerilog(CreateLogger());

var discordConfiguration = new DiscordConfiguration();
builder.Configuration.GetSection("Discord").Bind(discordConfiguration);

builder
    .Services
    .AddDatabase(builder.Configuration.GetConnectionString("Database")!)
    .AddLazyCache()
    .AddHttpContextAccessor()
    .AddHttpClient()
    .AddMediatR(typeof(ServiceResponse).Assembly, typeof(BotClient).Assembly)
    .AddDiscordBot(builder.Configuration)
    .AddSingleton(discordConfiguration)
    .AutoRegister()
    .AutoRegisterFromAccordServices();

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = DiscordAuthenticationDefaults.AuthenticationScheme;
    })
    .AddCookie()
    .AddDiscord(options =>
    {
        options.ClientId = builder.Configuration["Discord:ClientId"]!;
        options.ClientSecret = builder.Configuration["Discord:ClientSecret"]!;
        options.SaveTokens = true;
        options.AccessDeniedPath = "/welcome";
        
        options.Scope.Add("identify");

        options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "id", ClaimValueTypes.UInteger64);
        options.ClaimActions.MapJsonKey(ClaimTypes.Name, "username", ClaimValueTypes.String);
    });

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddMudServices();

if (!bool.TryParse(builder.Configuration["Discord:DisableBot"], out var disableDiscordBot) 
    || !disableDiscordBot)
{
    builder.Services
        .AddHostedService<BotHostedService>()
        .AddHostedService<CleanUpHelpForumHostedService>()
        .AddHostedService<RemindersHostedService>();
}

builder.Services.AddHostedService<EventQueueProcessor>();

var app = builder.Build();

Log.Information("Host built...");

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseCookiePolicy();
app.UseAuthentication();
app.UseAuthorization();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.MapGet("/login", async (context) => await context.ChallengeAsync(DiscordAuthenticationDefaults.AuthenticationScheme, new AuthenticationProperties { RedirectUri = "/" }));
app.MapGet("/logout", async (context) => await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme, new AuthenticationProperties { RedirectUri = "/welcome" }));

using (var scope = app.Services.CreateScope())
{
    Log.Information("Scope created...");
    await AccordContextExtensions.Migrate(scope.ServiceProvider.GetRequiredService<AccordContext>());
    Log.Information("Migrated!");
}

Log.Information("Ready to run");

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

    var sentrySection = builder.Configuration.GetSection("Sentry");
    var isSentryDisabled = bool.TryParse(sentrySection["Disable"], out var disable) && disable;

    if (!string.IsNullOrWhiteSpace(sentrySection["Dsn"]) && !isSentryDisabled)
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