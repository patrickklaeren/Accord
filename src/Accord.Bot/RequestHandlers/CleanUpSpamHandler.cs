using System;
using System.Threading;
using System.Threading.Tasks;
using Accord.Services;
using Accord.Services.Spam;
using MediatR;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Rest.Core;

namespace Accord.Bot.RequestHandlers;

public class CleanUpSpamHandler(DiscordConfiguration discordConfiguration, 
    IDiscordRestChannelAPI channelApi,
    IDiscordRestGuildAPI guildApi)
    : INotificationHandler<CleanUpSpamRequest>
{
    public async Task Handle(CleanUpSpamRequest request, CancellationToken cancellationToken)
    {
        var muteUntil = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(request.TimeoutInSeconds);

        await guildApi.ModifyGuildMemberAsync(
            new Snowflake(discordConfiguration.GuildId),
            new Snowflake(request.DiscordUserId),
            communicationDisabledUntil: muteUntil,
            reason: "Spam detected",
            ct: cancellationToken);
        
        foreach (var message in request.Messages)
        {
            await channelApi.DeleteMessageAsync(
                new Snowflake(message.DiscordChannelId),
                new Snowflake(message.DiscordMessageId),
                ct: cancellationToken);
        }
    }
}
