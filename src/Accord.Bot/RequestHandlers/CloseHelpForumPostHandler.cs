using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Abstractions.Objects;

namespace Accord.Bot.RequestHandlers;

public record CloseHelpForumPostRequest(IChannel Channel) : IRequest;

public class CloseHelpForumPostHandler(IDiscordRestChannelAPI channelApi, ILogger<CloseHelpForumPostHandler> logger) : IRequestHandler<CloseHelpForumPostRequest>
{
    private const string PREFIX = "✅";

    public async Task Handle(CloseHelpForumPostRequest request, CancellationToken cancellationToken)
    {
        if (request.Channel.Name.HasValue
            && !request.Channel.Name.Value!.StartsWith(PREFIX))
        {
            try
            {
                var newTitle = PREFIX + " " + request.Channel.Name.Value!.Replace("❔", string.Empty).Trim();
                await channelApi.ModifyThreadChannelAsync(request.Channel.ID, newTitle);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed changing title of forum post");
            }
        }

        var topMessage = await channelApi.GetChannelMessagesAsync(request.Channel.ID, around: request.Channel.ID, limit: 1, ct: cancellationToken);

        if (topMessage is { IsSuccess: true, Entity: [{ } messageToAction, ..] })
        {
            try
            {
                await channelApi.DeleteAllReactionsAsync(request.Channel.ID, messageToAction.ID, cancellationToken);
                await channelApi.CreateReactionAsync(request.Channel.ID, messageToAction.ID, PREFIX, cancellationToken);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed changing reactions of forum post");
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