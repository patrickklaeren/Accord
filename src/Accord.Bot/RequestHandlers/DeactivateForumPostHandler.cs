using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Remora.Discord.API.Abstractions.Rest;
using Serilog;
using Remora.Discord.API.Abstractions.Objects;

namespace Accord.Bot.RequestHandlers;

public record DeactivateForumPostRequest(IChannel Channel) : IRequest;

public class DeactivateForumPostHandler : AsyncRequestHandler<DeactivateForumPostRequest>
{
    private readonly IDiscordRestChannelAPI _channelApi;

    public DeactivateForumPostHandler(IDiscordRestChannelAPI channelApi)
    {
        _channelApi = channelApi;
    }

    private const string PREFIX = "❔";

    protected override async Task Handle(DeactivateForumPostRequest request, CancellationToken cancellationToken)
    {
        if (request.Channel.Name.HasValue
            && !request.Channel.Name.Value!.EndsWith(PREFIX))
        {
            try
            {
                var newTitle = PREFIX + " " + request.Channel.Name.Value!.Trim();
                await _channelApi.ModifyThreadChannelAsync(request.Channel.ID, newTitle, ct: cancellationToken);
            }
            catch (Exception e)
            {
                Log.Error(e, "Failed changing title of forum post");
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