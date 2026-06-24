using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using Accord.Domain.Model;
using Accord.Services.ChannelFlags;
using Accord.Services.RunOptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Accord.Services.Starboard;

public sealed record StarMessageRequest(
    ulong DiscordMessageId,
    ulong DiscordMessageChannelId,
    ulong? DiscordMessageChannelParentId,
    ulong DiscordMessageUserId) : IRequest;

public class StarMessageHandler(
    AccordContext db,
    ILogger<StarMessageHandler> logger,
    ChannelFlagService channelFlagService,
    RunOptionService runOptionService,
    IMediator mediator)
    : IRequestHandler<StarMessageRequest>
{
    public async Task Handle(StarMessageRequest request, CancellationToken cancellationToken)
    {
        var isStarboardChannel = await db.StarboardChannels
            .Where(x => x.DiscordStarboardChannelId == request.DiscordMessageChannelId)
            .AnyAsync(cancellationToken: cancellationToken);

        if (isStarboardChannel)
            return;

        var ignoredChannelIds = await channelFlagService.GetChannelIdsWithFlag(ChannelFlagType.IgnoreStarredMessages,
            cancellationToken);

        if (ignoredChannelIds.Contains(request.DiscordMessageChannelId)
            || (request.DiscordMessageChannelParentId is not null && ignoredChannelIds.Contains(request.DiscordMessageChannelParentId.Value)))
            return;

        var numberOfReactionsRequired = await runOptionService.GetOption<int>(RunOptionKey.NumberOfReactionsForStarboardEntry);

        var reactions = await mediator
            .Send(new GetDiscordMessageReactionsRequest(request.DiscordMessageChannelId, request.DiscordMessageId),
                cancellationToken);

        var numberOfReactions = reactions
            .Where(x => StarboardConstants.Emojis.Contains(x.EmojiName))
            .Sum(x => x.Count);

        var entry = await db
            .StarboardEntries
            .Include(x => x.Outputs)
            .Where(x => x.StarredDiscordMessageId == request.DiscordMessageId)
            .SingleOrDefaultAsync(cancellationToken: cancellationToken);

        if (entry is not null)
        {
            if (numberOfReactions >= numberOfReactionsRequired)
            {
                await HandleExistingStarboardEntry(entry, numberOfReactions, cancellationToken);
            }
            else
            {
                await HandleDeleteStarboardEntry(entry, cancellationToken);
            }
        }
        else if (numberOfReactions >= numberOfReactionsRequired)
        {
            await HandleNewStarboardEntry(request, numberOfReactions, cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task HandleNewStarboardEntry(StarMessageRequest request,
        int numberOfReactions,
        CancellationToken cancellationToken)
    {
        var channelIds = await GetStarboardChannelIdsToPostIn(request, cancellationToken);
        
        if (!channelIds.Any())
            return;

        var entry = new StarboardEntry
        {
            StarredDiscordMessageId = request.DiscordMessageId,
            StarredDiscordMessageChannelId = request.DiscordMessageChannelId,
            StarredDiscordUserId = request.DiscordMessageUserId,
            Stars = numberOfReactions
        };

        foreach (var channel in channelIds)
        {
            try
            {
                var discordStarboardMessageId = await mediator
                    .Send(new RelayNewStarboardEntryToDiscordRequest(channel,
                            request.DiscordMessageId,
                            request.DiscordMessageChannelId,
                            GetEmojiForNumberOfReactions(numberOfReactions),
                            numberOfReactions),
                        cancellationToken);

                if (discordStarboardMessageId is null)
                    continue;

                entry.Outputs.Add(new StarboardEntryOutput
                {
                    DiscordChannelId = channel,
                    DiscordMessageId = discordStarboardMessageId.Value
                });
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed posting starboard message {MessageId} to flagged channel {ChannelId}",
                    request.DiscordMessageId,
                    channel);
            }
        }

        db.StarboardEntries.Add(entry);
    }

    private async Task<IReadOnlyCollection<ulong>> GetStarboardChannelIdsToPostIn(StarMessageRequest request, CancellationToken cancellationToken)
    {
        if (request.DiscordMessageChannelParentId is not null)
        {
            var hasParentDesignatedStarboards = await db.StarboardChannels
                .Where(x => x.DiscordChannelBeingStarredInId == request.DiscordMessageChannelParentId)
                .AnyAsync(cancellationToken);

            if (hasParentDesignatedStarboards)
            {
                return await db.StarboardChannels
                    .Where(x => x.DiscordChannelBeingStarredInId == request.DiscordMessageChannelParentId)
                    .Select(x => x.DiscordStarboardChannelId)
                    .ToListAsync(cancellationToken: cancellationToken);
            }
        }
        
        var hasDesignatedStarboards = await db.StarboardChannels
            .Where(x => x.DiscordChannelBeingStarredInId == request.DiscordMessageChannelId)
            .AnyAsync(cancellationToken);

        return hasDesignatedStarboards
            ? await db.StarboardChannels
                .Where(x => x.DiscordChannelBeingStarredInId == request.DiscordMessageChannelId)
                .Select(x => x.DiscordStarboardChannelId)
                .ToListAsync(cancellationToken: cancellationToken)
            : await db.StarboardChannels
                .Where(x => x.DiscordChannelBeingStarredInId == null)
                .Select(x => x.DiscordStarboardChannelId)
                .ToListAsync(cancellationToken: cancellationToken);
    }

    private async Task HandleExistingStarboardEntry(StarboardEntry entry,
        int numberOfReactions,
        CancellationToken cancellationToken)
    {
        foreach (var output in entry.Outputs)
        {
            try
            {
                await mediator
                    .Send(new RelayExistingStarboardEntryToDiscordRequest(output.DiscordMessageId,
                            output.DiscordChannelId,
                            entry.StarredDiscordMessageId,
                            entry.StarredDiscordMessageChannelId,
                            GetEmojiForNumberOfReactions(numberOfReactions),
                            numberOfReactions),
                        cancellationToken);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed updating starboard message {MessageId} in flagged channel {ChannelId}",
                    output.DiscordMessageId,
                    output.DiscordChannelId);
            }
        }

        entry.Stars = numberOfReactions;
    }

    private async Task HandleDeleteStarboardEntry(StarboardEntry entry, CancellationToken cancellationToken)
    {
        foreach (var output in entry.Outputs)
        {
            try
            {
                await mediator.Send(new DeleteStarboardEntryToDiscordRequest(output.DiscordMessageId, output.DiscordChannelId),
                    cancellationToken);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed removing starboard message {MessageId} to flagged channel {ChannelId}",
                    output.DiscordMessageId,
                    output.DiscordChannelId);
            }

            db.StarboardEntryOutputs.Remove(output);
        }

        db.StarboardEntries.Remove(entry);
    }

    private static string GetEmojiForNumberOfReactions(int numberOfReactions)
    {
        return StarboardConstants.WeightedStars
            .Where(x => x.Key <= numberOfReactions)
            .MaxBy(x => x.Key)
            .Value;
    }
}