using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Services.Starboard;
using MediatR;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Rest.Core;

namespace Accord.Bot.Services;

public class GetMessageReactionsHandler(IDiscordRestChannelAPI channelApi) 
    : IRequestHandler<GetStarredDiscordMessageRequest, StarredDiscordMessageDto?>
{
    public async Task<StarredDiscordMessageDto?> Handle(GetStarredDiscordMessageRequest request, CancellationToken cancellationToken)
    {
        var channelSnowflake = new Snowflake(request.DiscordChannelId);
        var messageSnowflake = new Snowflake(request.DiscordMessageId);

        var message = await channelApi.GetChannelMessageAsync(channelSnowflake,
            messageSnowflake,
            cancellationToken);

        if (!message.IsSuccess)
            return null;

        List<ulong> reactionByUserIds = [];

        if (message.Entity.Reactions.HasValue && message is { Entity.Reactions.Value.Count: > 0 })
        {
            var discordReactions = await channelApi.GetReactionsAsync(channelSnowflake,
                messageSnowflake,
                StarboardConstants.EMOJI,
                ct: cancellationToken);

            if (discordReactions.IsSuccess)
            {
                reactionByUserIds = discordReactions
                    .Entity
                    .Select(x => x.ID.Value)
                    .ToList();
            }
        }

        return new StarredDiscordMessageDto(message.Entity.ID.Value, 
            message.Entity.Author.ID.Value,
            reactionByUserIds);
    }
}