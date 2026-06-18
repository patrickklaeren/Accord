using System.Threading;
using System.Threading.Tasks;
using Accord.Bot.Infrastructure;
using Accord.Services;
using Microsoft.Extensions.Logging;
using Remora.Discord.Commands.Services;
using Remora.Discord.Gateway;
using Remora.Discord.Gateway.Results;
using Remora.Rest.Core;
using Remora.Results;

// Gateway events: https://discord.com/developers/docs/topics/gateway#commands-and-events-gateway-events

namespace Accord.Bot;

[RegisterTransient]
public class BotClient(ILogger<BotClient> logger, DiscordGatewayClient discordGatewayClient, SlashService slashService, DiscordConfiguration discordConfiguration)
{

    public async Task Run(CancellationToken cancellationToken)
    {
        await InitialiseSlashCommands(cancellationToken);

        var runResult = await discordGatewayClient.RunAsync(cancellationToken);

        if (!runResult.IsSuccess)
        {
            switch (runResult.Error)
            {
                case ExceptionError exe:
                    {
                        logger.LogError(exe.Exception, "Exception during gateway connection: {ExceptionMessage}", exe.Message);
                        break;
                    }
                case GatewayWebSocketError:
                case GatewayDiscordError:
                    {
                        logger.LogError("Gateway error: {Message}", runResult.Error.Message);
                        break;
                    }
                default:
                    {
                        logger.LogError("Unknown error: {Message}", runResult.Error.Message);
                        break;
                    }
            }
        }
    }

    private async Task InitialiseSlashCommands(CancellationToken cancellationToken)
    {
        if (discordConfiguration.GuildId == default)
            return;

        var updateSlash = await slashService.UpdateSlashCommandsAsync(new Snowflake(discordConfiguration.GuildId), ct: cancellationToken);

        if (!updateSlash.IsSuccess)
        {
            logger.LogWarning("Failed to update slash commands: {Reason}", updateSlash.Error.Message);
        }
    }
}
