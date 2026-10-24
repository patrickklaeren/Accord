using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Services;
using Accord.Services.Starboard;
using MediatR;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Rest.Core;

namespace Accord.Bot.Services;

public class GetMessageReactionsHandler(IDiscordRestChannelAPI channelApi) 
    : IRequestHandler<GetDiscordMessageReactionsRequest, ReactedDiscordMessageDto?>
{
    public async Task<ReactedDiscordMessageDto?> Handle(GetDiscordMessageReactionsRequest request, CancellationToken cancellationToken)
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
            const int PAGE_SIZE = 100;
            Optional<Snowflake> after = default;

            while (true)
            {
                var discordReactions = await channelApi.GetReactionsAsync(channelSnowflake,
                    messageSnowflake,
                    request.Emoji,
                    after: after,
                    limit: PAGE_SIZE,
                    ct: cancellationToken);

                if (!discordReactions.IsSuccess)
                    return null;

                reactionByUserIds.AddRange(discordReactions.Entity.Select(x => x.ID.Value));

                if (discordReactions.Entity.Count < PAGE_SIZE)
                    break;

                after = discordReactions.Entity[^1].ID;
            }
        }

        return new ReactedDiscordMessageDto(message.Entity.ID.Value, 
            message.Entity.Author.ID.Value,
            reactionByUserIds);
    }
}
