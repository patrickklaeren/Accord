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
    secret: true,
    value: string.Empty,
    publishValueAsDefault: false);

var discordClientId = builder.AddParameter(
    name: "discord-client-id",
    secret: false,
    value: string.Empty,
    publishValueAsDefault: false);

var discordClientSecret = builder.AddParameter(
    name: "discord-client-secret",
    secret: true,
    value: string.Empty,
    publishValueAsDefault: false);

var discordGuildId = builder.AddParameter(
    name: "discord-guild-id",
    secret: false,
    value: string.Empty,
    publishValueAsDefault: false);

var discordHelpForumChannelId = builder.AddParameter(
    name: "discord-help-forum-channel-id",
    secret: false,
    value: string.Empty,
    publishValueAsDefault: false);

var discordCdnBaseUrl = builder.AddParameter(
    name: "discord-cdn-base-url",
    secret: false,
    value: "https://cdn.discordapp.com",
    publishValueAsDefault: true);

builder
    .AddDockerComposeEnvironment("compose")
    .WithDashboard(false);

var postgres = builder
    .AddPostgres("postgres", password: postgresPassword)
    .WithImageTag("16.5")
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
    .PublishAsDockerComposeService((_, service) => service.Name = "web");

builder.Build().Run();
