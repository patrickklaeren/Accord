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
    : INotificationHandler<CleanUpSpamInDiscordRequest>
{
    public async Task Handle(CleanUpSpamInDiscordRequest inDiscordRequest, CancellationToken cancellationToken)
    {
        var muteUntil = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(inDiscordRequest.TimeoutInSeconds);

        await guildApi.ModifyGuildMemberAsync(
            new Snowflake(discordConfiguration.GuildId),
            new Snowflake(inDiscordRequest.DiscordUserId),
            communicationDisabledUntil: muteUntil,
            reason: "Spam detected",
            ct: cancellationToken);
        
        foreach (var message in inDiscordRequest.Messages)
        {
            await channelApi.DeleteMessageAsync(
                new Snowflake(message.DiscordChannelId),
                new Snowflake(message.DiscordMessageId),
                ct: cancellationToken);
        }
    }
}
