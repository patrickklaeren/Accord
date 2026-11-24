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
      Ollama__BaseUrl: ${OLLAMA_BASEURL}
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

## Report to ModMail

Use two Ticket Tool panels so each report opens with the right details. In the Ticket Tool dashboard, select your server and open **Panel Configs**. Keep the original panel connected to the **Create ticket** button in `#report`. Select **Clone** and name the copy **Accord Reports** for right-click reports. Both panels can use the same support team and ticket category. [Panel settings](https://docs.tickettool.xyz/dashboard/panel-configs#clone)

### Create ticket in `#report`

1. On the **original panel**, open **Panel Settings → Command Style** and disable command-style ticket creation.
2. Open **Panel Settings → Form**, enable the form, and add a required **What happened?** question and an optional **Link to the message** question. Enable **Auto display results** so moderators see the submitted answers in the ticket. [Form settings](https://docs.tickettool.xyz/dashboard/panel-configs/form-options)
3. Under **General Ticket Options → Ticket → Ticket Message → Message Content**, replace the welcome text with the template below and save. A button click supplies form answers, so this template omits the command-only `{create.reason}` variable. [Ticket Message editor](https://docs.tickettool.xyz/dashboard/panel-configs/ticket-options#ticket-message)

```text
{ticket.user}, thanks for reporting this. A moderator will review the details below.
```

### Report to ModMail from a message

Use the **Accord Reports** clone for this flow. Disable its form under **Panel Settings → Form**. When a member right-clicks a message and selects **Apps → Report to ModMail**, Accord sends `$new <reporter ID> Reported message: <message link>` to Ticket Tool. The reporter becomes the ticket owner.

1. **Set the private command channel.** Choose one Discord text channel visible only to Accord, Ticket Tool, and admins so members cannot see the report commands. Grant both bots **View Channel** and **Send Messages** there. Give Ticket Tool **Manage Messages** in that channel so it can delete Accord's commands. In that channel, run `/channel-flag add type:ModMail`. Exactly one channel should have this flag.
2. **Authorize Accord.** In the Ticket Tool dashboard, select your server and open **Server Configs → Bot**. Add Accord's bot user ID and save. If you changed Ticket Tool's command prefix from `$`, set Accord's `Discord__TicketToolPrefix` to match. [Server configs](https://docs.tickettool.xyz/dashboard/server-configs#bot)
3. **Allow tickets on behalf of reporters.** Open **Panel Configs → Accord Reports → General Ticket Options → General → Support Team Roles**. Add a Discord role held by Accord and save. Ticket Tool requires a support team role to set another user as the owner with `$new`. [Support team roles](https://docs.tickettool.xyz/dashboard/panel-configs/general-options#support-team-roles) · [Ticket commands](https://docs.tickettool.xyz/general/commands#command-style-tickets)
4. **Enable command-style tickets and cleanup.** In **Accord Reports → Panel Settings → Command Style**, enable command-style ticket creation, turn off the option to watch all channels, and select the private channel from step 1. Only this panel should monitor that channel. Enable **Delete command after creating ticket** and save. Ticket Tool will remove Accord's `$new` message after it creates the ticket. [Command Style settings](https://docs.tickettool.xyz/dashboard/panel-configs/command-style-tickets#delete-command-after-creating-ticket)
5. **Show the reported message in the ticket.** In **General Ticket Options → Ticket → Ticket Message → Message Content**, paste the template below and save. Ticket Tool fills in `{create.reason}` with the reported message link supplied by Accord. [Ticket variables](https://docs.tickettool.xyz/dashboard/panel-configs/variables#special-variables)

```text
{ticket.user}, thanks for flagging this message. A moderator will review it.

{create.reason}
```

### Verify both panels

- Click **Create ticket** in `#report` and submit the form. The new ticket should show the manual welcome and your answers.
- Right-click a server message and select **Apps → Report to ModMail**. The new ticket should show `Reported message:` followed by a clickable link to that exact message. The private command channel should be clear of the processed `$new` message.

If a member cannot see **Report to ModMail**, check their **Use Application Commands** permission in the channel containing the message. Under **Server Settings → Integrations → your Accord app → Manage**, check the app permissions and any overrides on **Report to ModMail** for that member, their roles, and the channel. The Ticket Tool support team role in step 3 is for the bot. [Discord command permissions](https://docs.discord.com/developers/interactions/application-commands#permissions)

## Optional AI message analysis

Accord can categorise new member messages in its configured guild using Ollama as `Recruitment` (self-promotion or advertising) or `other`.

Set `Ollama__BaseUrl` (or `Ollama:BaseUrl` in configuration) to your Ollama server, for example `http://localhost:11434`. Accord defaults to `qwen3:0.6b`, which must already be available on the Ollama server.

An unset or invalid Ollama URL, a disabled option, or a missing run option disables analysis. Ollama failures, timeouts (30 seconds), and invalid classifications are logged and skipped. Analysis uses its own bounded background queue, so it does not delay message logging or existing spam checks. Direct messages, bots, system users, webhooks, and empty messages are excluded. The bot continues to operate without an Ollama endpoint.

## Credits

Notable dependencies for this project include:
- [Remora](https://github.com/Nihlus/Remora.Discord)
- [MediatR](https://github.com/jbogard/MediatR)
- [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/)
- [Npgsql EF Core Provider](https://www.npgsql.org/efcore/)
