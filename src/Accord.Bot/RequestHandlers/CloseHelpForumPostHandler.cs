using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Remora.Discord.API.Abstractions.Rest;
using Serilog;
using Remora.Discord.API.Abstractions.Objects;

namespace Accord.Bot.RequestHandlers;

public record CloseHelpForumPostRequest(IChannel Channel) : IRequest;

public class CloseHelpForumPostHandler : AsyncRequestHandler<CloseHelpForumPostRequest>
{
    private readonly IDiscordRestChannelAPI _channelApi;

    public CloseHelpForumPostHandler(IDiscordRestChannelAPI channelApi)
    {
        _channelApi = channelApi;
    }

    private const string PREFIX = "✅";

    protected override async Task Handle(CloseHelpForumPostRequest request, CancellationToken cancellationToken)
    {
        if (request.Channel.Name.HasValue
            && !request.Channel.Name.Value!.StartsWith(PREFIX))
        {
            try
            {
                var newTitle = PREFIX + " " + request.Channel.Name.Value!.Replace("❔", string.Empty).Trim();
                await _channelApi.ModifyThreadChannelAsync(request.Channel.ID, newTitle);
            }
            catch (Exception e)
            {
                Log.Error(e, "Failed changing title of forum post");
            }
        }

        var topMessage = await _channelApi.GetChannelMessagesAsync(request.Channel.ID, around: request.Channel.ID, limit: 1, ct: cancellationToken);

        if (topMessage is { IsSuccess: true, Entity: [{ } messageToAction, ..] })
        {
            try
            {
                await _channelApi.DeleteAllReactionsAsync(request.Channel.ID, messageToAction.ID, cancellationToken);
                await _channelApi.CreateReactionAsync(request.Channel.ID, messageToAction.ID, PREFIX, cancellationToken);
            }
            catch (Exception e)
            {
                Log.Error(e, "Failed changing reactions of forum post");
            }
        }

        try
        {
            await _channelApi.ModifyThreadChannelAsync(request.Channel.ID, isArchived: true, ct: cancellationToken);
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed archiving forum post");
        }
    }
}