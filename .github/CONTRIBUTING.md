# How do I contribute

Accord is actively developed with both Rider and Visual Studio. It is recommended you use either IDE for a positive experience. However, Accord is built entirely on .NET and therefore works anywhere `dotnet build` can be executed.

You can grab an unassigned issue and comment on it to indicate your interest on championing it. Alternatively, if you have a suggestion for a new feature and want to champion this, create a new issue and it can be discussed in the repository.

Please do not submit pull requests without prior conversation with maintainers in the repository. Your contribution may not be accepted, as ultimately it has to be maintained through, potentially, the lifetime of this codebase.

## Development ethos

Keep things short, simple and maintainable. No pointless abstractions or complicated chains. Move fast, break and innovate. This is a Discord bot first and foremost and it strives for simplicity.

## Quickstart

**What you'll need**

- [Latest .NET SDK](https://dotnet.microsoft.com/en-us/download)
- [Aspire CLI](https://aspire.dev/get-started/install-cli/)
- Docker
- [Discord Bot](https://discord.com/developers/applications)

**How to get Accord running in development**

- Set up a bot account on the [Discord developer portal](https://discord.com/developers/applications)
    - Ensure you have the following priviledged gateway intents enabled:
        - Presence Intent
        - Server Members Intent
- Clone/fork the repository from `main` branch
- Get the Id of the Discord Guild you will be testing the bot in, for the purposes of Slash command updating
- Get your bot token from the [Discord developer portal](https://discord.com/developers/applications)
- Run via Aspire `Accord.AppHost`, set up the parameters as you see fit

**Invite your bot**

(Change your client Id to that of your application's)

```https://discord.com/oauth2/authorize?client_id=CLIENT_ID&scope=bot%20applications.commands&permissions=1573252310```

This ensures the bot has the minimum required permissions and can manage Slash commands on the guild.

Start via `Accord.AppHost`. This will apply migrations automatically via Entity Framework.