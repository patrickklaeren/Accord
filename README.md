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
      AppBaseUrl: ${APPBASEURL}
      ReplBaseUrl: ${REPLBASEURL}
      PasteBaseUrl: ${PASTEBASEURL}
      Discord__ClientSecret: ${DISCORD_CLIENTID}
      Discord__ClientId: ${DISCORD_CLIENTID}
      Discord__GuildId: ${DISCORD_GUILDID}
      Discord__BotToken: ${DISCORD_BOTTOKEN}
      Shlink__BaseUrl: ${SHLINK_BASEURL}
      Shlink__APIKey: ${SHLINK_APIKEY}
```

Accord can optionally connect to a C# REPL service, via the REPL module. You can self-host this:

```yml
  repl:
    image: ghcr.io/discord-csharp/csharprepl:latest
    environment:
      - ASPNETCORE_URLS=http://+:31337
```

When using the REPL service, Accord also makes use of a "paste service", in this case: https://github.com/matze/wastebin. You can self-host this:

```yml
  wastebin:
    image: quxfoo/wastebin:latest
    ports:
      - "8088:8088"
```

Accord also has a link shortening service via https://github.com/shlinkio/shlink. You can self-host this:

```yml
  shlink:
    image: "ghcr.io/shlinkio/shlink:latest"
    ports:
      - "8080:8080"
```

| Variable                    | Description                                                                                                                                                                                                                                                                                           |
|-----------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `ConnectionStrings__accord` | Connection string to the PostgreSQL database.                                                                                                                                                                                                                                                         |
| `AppBaseUrl`                | Base URL to the Accord website. If you do not intend to use the web frontend, set the value to `http://`. Otherwise, configure it to be your main domain that will point to the service.                                                                                                              |
| `ReplBaseUrl`               | Base URL to the REPL service used by the REPL module. If you do not intend to use this service, set the value to `http://`. Otherwise, configure it to point to the appropriate service endpoint.                                                                                                     |
| `PasteBaseUrl`              | Base URL to the paste service. If you do not intend to use this service, set the value to `http://`. Otherwise, configure it to point to the appropriate service endpoint. If the REPL service is configured, but no paste service is provided, the REPL service may fail to post results in Discord. |
| `Discord__ClientId`         | Client ID of your Discord application.                                                                                                                                                                                                                                                                |
| `Discord__ClientSecret`     | Client secret of your Discord application.                                                                                                                                                                                                                                                            |
| `Discord__GuildId`          | Guild (server) snowflake ID that the bot will connect to.                                                                                                                                                                                                                                             |
| `Discord__BotToken`         | Bot token of your Discord application.                                                                                                                                                                                                                                                                |
| `Shlink__BaseUrl`           | Base URL to the Shlink instance for link shortening. If you do not intend to use this service, set the value to `http://`. Otherwise, configure it to point to the appropriate service endpoint.                                                                                                      |
| `Shlink__ApiKey`            | API Key generated in your Shlink instance in order for the Rest API to authenticate requests.                                                                                                                                                                                                         |

This bot is intended for **single-guild** usage.

## Credits

Notable dependencies for this project include:
- [Remora](https://github.com/Nihlus/Remora.Discord)
- [MediatR](https://github.com/jbogard/MediatR)
- [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/)
- [Npgsql EF Core Provider](https://www.npgsql.org/efcore/)