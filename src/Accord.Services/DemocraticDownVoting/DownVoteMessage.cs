using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain.Model;
using Accord.Services.Permissions;
using Accord.Services.RunOptions;
using MediatR;

namespace Accord.Services.DemocraticDownVoting;

public sealed record DownVoteMessageRequest(PermissionUser UserBeingDownVoted,
    DateTimeOffset DiscordMessageSentAtDateTime,
    ulong DiscordMessageId, 
    ulong DiscordChannelId) : IRequest;

internal class DownVoteMessageHandler(UserPermissionService userPermissionService, 
    RunOptionService runOptionService,
    IMediator mediator) 
    : IRequestHandler<DownVoteMessageRequest>
{
    public async Task Handle(DownVoteMessageRequest request, CancellationToken cancellationToken)
    {
        const int THRESHOLD_IN_MINUTES = 5;
        var threshold = DateTimeOffset.Now.AddMinutes(-THRESHOLD_IN_MINUTES);

        if (request.DiscordMessageSentAtDateTime < threshold)
        {
            return;
        }
        
        if (request.UserBeingDownVoted.IsAdministrator || request.UserBeingDownVoted.IsBotSelf)
        {
            return;
        }

        if (!await runOptionService.GetOption<bool>(RunOptionKey.DemocraticDownVotingEnabled))
        {
            return;
        }

        if (await userPermissionService.HasPermission(request.UserBeingDownVoted, PermissionType.BypassDownVotes))
        {
            return;
        }

        var numberOfReactionsRequired = await runOptionService.GetOption<int>(RunOptionKey.DemocraticDownVotesRequired);

        var messageWithReactions = await mediator
            .Send(new GetDiscordMessageReactionsRequest(request.DiscordChannelId, request.DiscordMessageId, DemocraticDownVotingConstants.EMOJI),
                cancellationToken);

        if (messageWithReactions is null)
            return;

        var downvotedByUserIds = messageWithReactions
            .ReactedByUserIds
            .Where(userId => userId != request.UserBeingDownVoted.DiscordUserId)
            .ToList();
        
        var numberOfReactions = downvotedByUserIds.Count;

        if (numberOfReactions < numberOfReactionsRequired)
            return;

        await mediator
            .Send(new RelayDownVotedMessageToDiscordRequest(request.DiscordMessageId, request.DiscordChannelId, request.UserBeingDownVoted.DiscordUserId, downvotedByUserIds),
                cancellationToken);
    }
}