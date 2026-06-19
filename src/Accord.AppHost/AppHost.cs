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

var discordHelpForumChannelId = builder.AddParameter(
    name: "discord-help-forum-channel-id",
    secret: false);

var discordCdnBaseUrl = builder.AddParameter(
    name: "discord-cdn-base-url",
    secret: false,
    value: "https://cdn.discordapp.com",
    publishValueAsDefault: true);

var replBaseUrl = builder.AddParameter(
    name: "repl-base-url",
    secret: false);

builder
    .AddDockerComposeEnvironment("compose")
    .WithDashboard(false);

var postgres = builder
    .AddPostgres("postgres", password: postgresPassword)
    .WithImageTag("18.4")
    .WithDataVolume("accord-postgres-data")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithPgAdmin()
    .AddDatabase("accord");

builder
    .AddProject<Projects.Accord_Web>("web")
    .WithReference(postgres)
    .WaitFor(postgres)
    .WithEnvironment("Sentry__Dsn", sentryDsn)
    .WithEnvironment("Sentry__Environment", sentryEnvironment)
    .WithEnvironment("Discord__BotToken", discordBotToken)
    .WithEnvironment("Discord__ClientId", discordClientId)
    .WithEnvironment("Discord__ClientSecret", discordClientSecret)
    .WithEnvironment("Discord__GuildId", discordGuildId)
    .WithEnvironment("Discord__HelpForumChannelId", discordHelpForumChannelId)
    .WithEnvironment("Discord__CdnBaseUrl", discordCdnBaseUrl)
    .WithEnvironment("ReplBaseUrl", replBaseUrl)
    .PublishAsDockerComposeService((_, service) => service.Name = "web");

builder.Build().Run();
