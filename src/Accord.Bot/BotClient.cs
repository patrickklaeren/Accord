using System.Threading;
using System.Threading.Tasks;
using Accord.Bot.Infrastructure;
using Microsoft.Extensions.Logging;
using Remora.Discord.Commands.Services;
using Remora.Discord.Gateway;
using Remora.Discord.Gateway.Results;
using Remora.Rest.Core;
using Remora.Results;

// Gateway events: https://discord.com/developers/docs/topics/gateway#commands-and-events-gateway-events

namespace Accord.Bot;

[AutoConstructor, RegisterTransient]
public partial class BotClient
{
    private readonly ILogger<BotClient> _logger;
    private readonly DiscordGatewayClient _discordGatewayClient;
    private readonly SlashService _slashService;
    private readonly DiscordConfiguration _discordConfiguration;

    public async Task Run(CancellationToken cancellationToken)
    {
        await InitialiseSlashCommands(cancellationToken);

        var runResult = await _discordGatewayClient.RunAsync(cancellationToken);

        if (!runResult.IsSuccess)
        {
            switch (runResult.Error)
            {
                case ExceptionError exe:
                    {
                        _logger.LogError(exe.Exception, "Exception during gateway connection: {ExceptionMessage}", exe.Message);
                        break;
                    }
                case GatewayWebSocketError:
                case GatewayDiscordError:
                    {
                        _logger.LogError("Gateway error: {Message}", runResult.Error.Message);
                        break;
                    }
                default:
                    {
                        _logger.LogError("Unknown error: {Message}", runResult.Error.Message);
                        break;
                    }
            }
        }
    }

    private async Task InitialiseSlashCommands(CancellationToken cancellationToken)
    {
        if (_discordConfiguration.GuildId == default)
            return;

        var updateSlash = await _slashService.UpdateSlashCommandsAsync(new Snowflake(_discordConfiguration.GuildId), ct: cancellationToken);

        if (!updateSlash.IsSuccess)
        {
            _logger.LogWarning("Failed to update slash commands: {Reason}", updateSlash.Error.Message);
        }
    }
}