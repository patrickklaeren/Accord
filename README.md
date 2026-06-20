# Accord
<p align="center">
    <img alt='Accord Logo' src='branding/readme-banner.png'/>
</p>

## Reach accord, prevent discord, on Discord.

A C# .NET Discord bot with moderation, XP/guild participation and utilities aimed at all users within a guild. This is not a hosted bot, it is intended to be self hosted.

## How to self host

You can build from source or host via the published Docker image. An example `docker-compose.yml` is below.

```yml
services:
  postgres:
    image: postgres
    restart: always
    environment:
      POSTGRES_USER: ${POSTGRES_USER}
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}

  bot:
    image: ghcr.io/patrickklaeren/accord:main
    depends_on:
      - postgres
    restart: always
    environment:
      ConnectionStrings__accord: ${CONNECTIONSTRINGS_DATABASE}
      ReplBaseUrl: ${REPLBASEURL}
      Discord__ClientSecret: ${DISCORD_CLIENTID}
      Discord__ClientId: ${DISCORD_CLIENTID}
      Discord__GuildId: ${DISCORD_GUILDID}
      Discord__BotToken: ${DISCORD_BOTTOKEN}
```

Accord can optionally connect to a C# REPL service, via the REPL module. You can self-host this:

```yml
  repl:
    image: ghcr.io/discord-csharp/csharprepl:latest
    environment:
      - ASPNETCORE_URLS=http://+:31337
```

| Variable | Description |
|----------------------|-------------|
| `ConnectionStrings__accord` | Connection string to the PostgreSQL database. |
| `ReplBaseUrl` | Base URL to the REPL service used by the REPL module. If you do not intend to use this service, set the value to `http://`. Otherwise, configure it to point to the appropriate REPL service endpoint. |
| `Discord__ClientId` | Client ID of your Discord application. |
| `Discord__ClientSecret` | Client secret of your Discord application. |
| `Discord__GuildId` | Guild (server) snowflake ID that the bot will connect to. |
| `Discord__BotToken` | Bot token of your Discord application. |

This bot is intended for **single-guild** usage.