# Accord

![Accord Logo](branding/readme-logo.png)

## Reach accord, prevent discord, on Discord.

A C# .NET 5 Discord bot with moderation, XP/guild participation and utilities aimed at all users within a guild. This is not a hosted bot, it is intended to be self hosted.

### How do I contribute

Accord is actively developed with both Rider and Visual Studio 2019. It is recommended you use either IDE for a positive experience. However, Accord is built entirely on .NET 5 and therefore works anywhere `dotnet build` can be executed.

You can grab an unassigned issue and comment on it to indicate your interest on championing it. Alternatively, if you have a suggestion for a new feature and want to champion this, create a new issue and it can be discussed in the repository.

**Development ethos**

Keep things short, simple and maintainable. No pointless abstractions or complicated chains. Move fast, break and innovate. This is a Discord bot first and foremost and it strives for simplicity.

**What you'll need**

- Latest .NET 5 SDK
- SQL Server Developer Edition (Docker image available)
- Discord Bot account

**How to get Accord running in development**

- Set up a bot account on the [Discord developer portal](https://discord.com/developers/applications)
    - Ensure you have the following priviledged gateway intents enabled:
        - Presence Intent
        - Server Members Intent
- Clone/fork the repository from `main` branch
- Get the Id of the Discord Guild you will be testing the bot in, for the purposes of Slash command updating
- Get your bot token from the [Discord developer portal](https://discord.com/developers/applications)
- Get your OAuth2 ClientId & ClientSecret from the OAuth2 tab
- Set up configurations for development, using [user-secrets](https://docs.microsoft.com/en-us/aspnet/core/security/app-secrets)
    - `dotnet user-secrets set DiscordConfiguration:GuildId GUILD_ID`
    - `dotnet user-secrets set DiscordConfiguration:BotToken BOT_TOKEN`
    - `dotnet user-secrets set DiscordConfiguration:ClientId CLIENT_ID`
    - `dotnet user-secrets set DiscordConfiguration:ClientSecret CLIENT_SECRET`

By default the bot will look for a SQL Server instance running on `localhost`. If your instance is not on `localhost` or has an otherwise differing connection string, set the `ConnectionStrings:Database` secret.

**Invite your bot**

(Change your client Id to that of your application's)

```https://discord.com/oauth2/authorize?client_id=CLIENT_ID&scope=bot%20applications.commands&permissions=1573252310```

This ensures the bot has the minimum required permissions and can manage Slash commands on the guild.

Start the bot. This will apply migrations automatically via Entity Framework.

### How to self host

Currently you will need to build from source. There are no distributions at this time.

**Requirements**
- SQL Server
- Web host for ASP.NET Core

Set environment variables for `ConnectionStrings:Database`, `DiscordConfiguration:GuildId`, `DiscordConfiguration:BotToken`.

This bot is intended for single-guild usage.

### Credits

Notable dependencies for this project include:
- [Remora](https://github.com/Nihlus/Remora.Discord)
- [MediatR](https://github.com/jbogard/MediatR)
- [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/)