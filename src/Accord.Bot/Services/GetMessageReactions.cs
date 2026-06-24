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
    : IRequestHandler<GetDiscordMessageReactionsRequest, IReadOnlyCollection<MessageReactionDto>>
{
    public async Task<IReadOnlyCollection<MessageReactionDto>> Handle(GetDiscordMessageReactionsRequest request, CancellationToken cancellationToken)
    {
        var channelSnowflake = new Snowflake(request.DiscordChannelId);
        var messageSnowflake = new Snowflake(request.DiscordMessageId);

        var message = await channelApi.GetChannelMessageAsync(channelSnowflake,
            messageSnowflake,
            cancellationToken);

        if (message is { Entity: { Reactions.HasValue: true } })
        {
            return message
                .Entity
                .Reactions
                .Value
                .Where(x => x.Emoji.Name.HasValue)
                .Select(x => new MessageReactionDto(x.Emoji.Name.Value!, x.Count))
                .ToList();
        }

        return [];
    }
}