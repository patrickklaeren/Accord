using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Accord.Services.DemocraticDownVoting;
using MediatR;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Rest.Core;

namespace Accord.Bot.RequestHandlers;

public class RelayDownVotedMessageToDiscordHandler(
    IDiscordRestChannelAPI channelApi)
    : IRequestHandler<RelayDownVotedMessageToDiscordRequest>
{
    public async Task Handle(RelayDownVotedMessageToDiscordRequest notification, CancellationToken cancellationToken)
    {
        var channelSnowflake = new Snowflake(notification.DownVotedMessageChannelId);
        var messageSnowflake = new Snowflake(notification.DownVotedMessageId);

        var downvotedBy = string.Join(", ", notification.DownVotedByUserIds.Select(DiscordFormatter.UserIdToMention));

        var deleteResponse = await channelApi
            .DeleteMessageAsync(channelSnowflake,
                messageSnowflake,
                reason: $"Down voted by {downvotedBy}",
                ct: cancellationToken);

        if (!deleteResponse.IsSuccess)
            return;
        
        var authorMention = DiscordFormatter.UserIdToMention(notification.DownVotedMessageAuthorId);
        var smallMarkdown = DiscordFormatter.ToSmallMarkdown("If you think this is not right, ping a moderator.");
        await channelApi.CreateMessageAsync(channelSnowflake, $"{authorMention} your message was downvoted to be removed.{Environment.NewLine}{smallMarkdown}", ct: cancellationToken);
    }
}
