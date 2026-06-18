using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Abstractions.Objects;

namespace Accord.Bot.RequestHandlers;

public record DeactivateForumPostRequest(IChannel Channel) : IRequest;

public class DeactivateForumPostHandler(IDiscordRestChannelAPI channelApi, ILogger<DeactivateForumPostHandler> logger) : IRequestHandler<DeactivateForumPostRequest>
{

    private const string PREFIX = "❔";

    public async Task Handle(DeactivateForumPostRequest request, CancellationToken cancellationToken)
    {
        if (request.Channel.Name.HasValue
            && !request.Channel.Name.Value!.EndsWith(PREFIX))
        {
            try
            {
                var newTitle = PREFIX + " " + request.Channel.Name.Value!.Trim().Replace(PREFIX, string.Empty);
                await channelApi.ModifyThreadChannelAsync(request.Channel.ID, newTitle, ct: cancellationToken);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed changing title of forum post");
            }
        }

        try
        {
            await channelApi.ModifyThreadChannelAsync(request.Channel.ID, isArchived: true, ct: cancellationToken);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed archiving forum post");
        }
    }
}