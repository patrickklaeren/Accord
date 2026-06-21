using System;
using System.Linq;
using System.Security.Claims;
using Accord.Bot;
using Accord.Bot.HostedServices;
using Accord.Bot.Infrastructure;
using Accord.Domain;
using Accord.Services;
using Accord.Services.CodeEvaluation;
using AspNet.Security.OAuth.Discord;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MudBlazor.Services;
using Polly;
using Polly.Extensions.Http;
using Sentry;

var builder = WebApplication
    .CreateBuilder(args);

builder.WebHost.UseSentry(x => x.SetBeforeSend(BeforeSend));

var discordConfiguration = new DiscordConfiguration();
builder.Configuration.GetSection("Discord").Bind(discordConfiguration);

builder
    .Services
    .AddDatabase(builder.Configuration.GetConnectionString("accord")!)
    .AddLazyCache()
    .AddHttpContextAccessor()
    .AddHttpClient()
    .AddMediatR(d => d.RegisterServicesFromAssemblies(typeof(BotClient).Assembly, typeof(ServiceResponse).Assembly))
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

builder.Services.AddHttpClient<CSharpReplApiService>(x =>
    {
        x.BaseAddress = new Uri(builder.Configuration["ReplBaseUrl"]!);
    })
    .AddPolicyHandler(HttpPolicyExtensions.HandleTransientHttpError().WaitAndRetryAsync(2, _ => TimeSpan.FromSeconds(5)));

if (!string.IsNullOrWhiteSpace(builder.Configuration["Discord:BotToken"]))
{
    builder.Services
        .AddHostedService<BotHostedService>()
        .AddHostedService<CleanUpHelpForumHostedService>()
        .AddHostedService<RemindersHostedService>();
}

builder.Services.AddHostedService<EventQueueProcessor>();

var app = builder.Build();

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
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Scope created...");
    await AccordContextExtensions.Migrate(scope.ServiceProvider.GetRequiredService<AccordContext>());
    logger.LogInformation("Migrated!");
}

await app.RunAsync();

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