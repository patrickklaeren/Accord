var builder = DistributedApplication.CreateBuilder(args);

var postgresPassword = builder.AddParameter(
    name: "postgres-password",
    secret: true);

var sentryDsn = builder.AddParameter(
    name: "sentry-dsn",
    secret: false,
    value: string.Empty,
    publishValueAsDefault: false);

var sentryEnvironment = builder.AddParameter(
    name: "sentry-environment",
    secret: false,
    value: "dev",
    publishValueAsDefault: false);

var discordBotToken = builder.AddParameter(
    name: "discord-bot-token",
    secret: true)
    .WithDescription("Token for the Discord bot to run under, obtained from the [Discord Developer Portal](https://discord.com/developers/applications/)");

var discordClientId = builder.AddParameter(
    name: "discord-client-id",
    secret: false)
    .WithDescription("Discord Client ID used for Discord authentication for the frontend, obtained from the [Discord Developer Portal](https://discord.com/developers/applications/)");

var discordClientSecret = builder.AddParameter(
    name: "discord-client-secret",
    secret: true)
    .WithDescription("Discord Client Secret used for Discord authentication for the frontend, obtained from the [Discord Developer Portal](https://discord.com/developers/applications/)");

var discordGuildId = builder.AddParameter(
    name: "discord-guild-id",
    secret: false);

var discordCdnBaseUrl = builder.AddParameter(
    name: "discord-cdn-base-url",
    secret: false,
    value: "https://cdn.discordapp.com",
    publishValueAsDefault: true);

var shlinkApiKey = builder.AddParameter(
    name: "shlink-api-key",
    secret: true);

var appBaseUrl = builder.AddParameter(
    name: "app-base-url",
    secret: false,
    value: "https://localhost:7568");

builder
    .AddDockerComposeEnvironment("compose")
    .WithDashboard(false);

var postgres = builder
    .AddPostgres("postgres", password: postgresPassword)
    .WithImageTag("18.4")
    .WithDataVolume("accord-postgres-data")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithPgAdmin();

var accordDatabase = postgres.AddDatabase("accord");
var shlinkDatabase = postgres.AddDatabase("shlink");

var repl = builder
    .AddContainer("repl-app", "ghcr.io/discord-csharp/csharprepl", "latest")
    .WithEnvironment("ASPNETCORE_URLS", "http://+:31337")
    .WithHttpEndpoint(port: 31337, targetPort: 31337, name: "http");

var paste = builder
    .AddContainer("paste-app", "quxfoo/wastebin", "latest")
    .WithHttpEndpoint(port: 8088, targetPort: 8088, name: "http");

var shlink = builder
    .AddContainer("shlink-app", "ghcr.io/shlinkio/shlink", "latest")
    .WithReference(shlinkDatabase)
    .WaitFor(shlinkDatabase)
    .WithEnvironment("DEFAULT_DOMAIN", "localhost:8089")
    .WithEnvironment("IS_HTTPS_ENABLED", "false")
    .WithEnvironment("DB_DRIVER", "postgres")
    .WithEnvironment("DB_PASSWORD", postgresPassword)
    .WithEnvironment("DB_USER", postgres.Resource.UserNameReference)
    .WithEnvironment("DB_PORT", postgres.Resource.Port)
    .WithEnvironment("DB_HOST", postgres.Resource.Host)
    .WithEnvironment("INITIAL_API_KEY", shlinkApiKey)
    .WithHttpEndpoint(port: 8089, targetPort: 8080, name: "http");

builder
    .AddProject<Projects.Accord_Web>("app")
    .WithReference(accordDatabase)
    .WaitFor(accordDatabase)
    .WaitFor(paste)
    .WaitFor(repl)
    .WaitFor(shlink)
    .WithEnvironment("Sentry__Dsn", sentryDsn)
    .WithEnvironment("Sentry__Environment", sentryEnvironment)
    .WithEnvironment("Discord__BotToken", discordBotToken)
    .WithEnvironment("Discord__ClientId", discordClientId)
    .WithEnvironment("Discord__ClientSecret", discordClientSecret)
    .WithEnvironment("Discord__GuildId", discordGuildId)
    .WithEnvironment("Discord__CdnBaseUrl", discordCdnBaseUrl)
    .WithEnvironment("AppBaseUrl", appBaseUrl)
    .WithEnvironment("ReplBaseUrl", repl.GetEndpoint("http"))
    .WithEnvironment("PasteBaseUrl", paste.GetEndpoint("http"))
    .WithEnvironment("Shlink__BaseUrl", shlink.GetEndpoint("http"))
    .WithEnvironment("Shlink__ApiKey", shlinkApiKey)
    .PublishAsDockerComposeService((_, service) => service.Name = "app");

builder.Build().Run();
