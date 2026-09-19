using System;
using System.Linq;
using System.Security.Claims;
using Accord.Bot;
using Accord.Bot.HostedServices;
using Accord.Domain;
using Accord.Services;
using Accord.Services.CodeEvaluation;
using Accord.Services.DemocraticDownVoting;
using Accord.Services.Godbolt;
using Accord.Services.Paste;
using Accord.Services.Rss;
using Accord.Services.Shlink;
using Accord.Services.Starboard;
using Accord.Services.Spam;
using AspNet.Security.OAuth.Discord;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MudBlazor.Services;
using Polly;
using Polly.Extensions.Http;

var builder = WebApplication
    .CreateBuilder(args);

builder.WebHost.UseSentry();

var accordConfiguration = new AccordConfiguration();
builder.Configuration.Bind(accordConfiguration);

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
    .AddSingleton(accordConfiguration)
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

builder.Services.AddHttpClient<PasteApiService>(x =>
    {
        x.BaseAddress = new Uri(builder.Configuration["PasteBaseUrl"]!);
    })
    .AddPolicyHandler(HttpPolicyExtensions.HandleTransientHttpError().WaitAndRetryAsync(2, _ => TimeSpan.FromSeconds(5)));

builder.Services.AddHttpClient<GodboltApiService>(x =>
    {
        x.DefaultRequestHeaders.Add("Accept", "text/plain");
    })
    .AddPolicyHandler(HttpPolicyExtensions.HandleTransientHttpError().WaitAndRetryAsync(2, _ => TimeSpan.FromSeconds(5)));

builder.Services.AddHttpClient<ShlinkApiService>(x =>
    {
        x.BaseAddress = new Uri(builder.Configuration["Shlink:BaseUrl"]!);
        x.DefaultRequestHeaders.Add("X-Api-Key", builder.Configuration["Shlink:ApiKey"]);
    })
    .AddPolicyHandler(HttpPolicyExtensions.HandleTransientHttpError().WaitAndRetryAsync(2, _ => TimeSpan.FromSeconds(5)));

builder.Services.AddHttpClient<RssFeedReaderService>(x =>
    {
        x.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/149.0.0.0 Safari/537.36 Edg/149.0.0.0");
    })
    .AddPolicyHandler(HttpPolicyExtensions.HandleTransientHttpError().WaitAndRetryAsync(2, _ => TimeSpan.FromSeconds(5)));

if (!string.IsNullOrWhiteSpace(builder.Configuration["Discord:BotToken"]))
{
    builder.Services
        .AddHostedService<BotHostedService>()
        .AddHostedService<RemindersHostedService>()
        .AddHostedService<RssPollingHostedService>()
        .AddHostedService<VoiceChannelLeaseCleanupHostedService>();
}

builder.Services.AddHostedService<CoreEventQueueProcessor>();
builder.Services.AddHostedService<StarboardEventQueueProcessor>();
builder.Services.AddHostedService<SpamEventQueueProcessor>();
builder.Services.AddHostedService<DemocraticDownVotingEventQueueProcessor>();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

var app = builder.Build();

app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.UseCookiePolicy();
app.UseAuthentication();
app.UseAuthorization();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.MapGet("/login", async (context) => await context.ChallengeAsync(DiscordAuthenticationDefaults.AuthenticationScheme, new AuthenticationProperties { RedirectUri = "/" }));
app.MapGet("/logout", async (context) => await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme, new AuthenticationProperties { RedirectUri = "/welcome" }));

app.MapGet("/debug/all-headers", (HttpContext context) =>
    context.Request.Headers.ToDictionary(
        x => x.Key,
        x => x.Value.ToString()));

app.MapGet("/debug/request", (HttpContext context) => new
{
    Scheme = context.Request.Scheme,
    Host = context.Request.Host.ToString(),
    RedirectUri = $"{context.Request.Scheme}://{context.Request.Host}/signin-discord"
});

using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Scope created...");
    await AccordContextExtensions.Migrate(scope.ServiceProvider.GetRequiredService<AccordContext>());
    logger.LogInformation("Migrated!");
}

await app.RunAsync();