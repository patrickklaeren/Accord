using System.Threading;
using System.Threading.Tasks;
using Accord.Services;
using Accord.Services.Users;
using MediatR;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Rest.Core;

namespace Accord.Bot.RequestHandlers;

public class VoiceUnmuteUserHandler(DiscordConfiguration discordConfiguration,
    IDiscordRestGuildAPI guildApi)
    : INotificationHandler<VoiceUnmuteUserInDiscordRequest>
{
    public async Task Handle(VoiceUnmuteUserInDiscordRequest userInDiscordRequest, CancellationToken cancellationToken)
    {
        await guildApi.ModifyGuildMemberAsync(
            new Snowflake(discordConfiguration.GuildId),
            new Snowflake(userInDiscordRequest.DiscordUserId),
            isMuted: false,
            reason: "Automatically unmuted",
            ct: cancellationToken);
    }
}
