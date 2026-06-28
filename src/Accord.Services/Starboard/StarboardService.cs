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

[RegisterScoped]
internal class StarboardService(IMediator mediator,
    AccordContext db,
    ChannelFlagService channelFlagService,
    RunOptionService runOptionService,
    ILogger<StarboardService> logger)
{
    public async Task HandleMessageForStarboard(ulong messageId, 
        ulong channelId, 
        ulong? parentChannelId, 
        ulong userId, 
        CancellationToken cancellationToken)
    {
        var isStarboardChannel = await db.StarboardChannels
            .Where(x => x.DiscordStarboardChannelId == channelId)
            .AnyAsync(cancellationToken: cancellationToken);

        if (isStarboardChannel)
            return;

        var ignoredChannelIds = await channelFlagService.GetChannelIdsWithFlag(ChannelFlagType.IgnoreStarredMessages,
            cancellationToken);

        if (ignoredChannelIds.Contains(channelId)
            || (parentChannelId is not null && ignoredChannelIds.Contains(parentChannelId.Value)))
            return;

        var numberOfReactionsRequired = await runOptionService.GetOption<int>(RunOptionKey.StarboardNumberOfReactionsRequired);
        var allowSelfStarring = await runOptionService.GetOption<bool>(RunOptionKey.StarboardSelfStarring);

        var message = await mediator
            .Send(new GetStarredDiscordMessageRequest(channelId, messageId),
                cancellationToken);

        if (message is null)
            return;

        var numberOfReactions = message
            .StarredByUserIds
            .Count(x => allowSelfStarring || x != message.AuthorId);

        var entry = await db
            .StarboardEntries
            .Include(x => x.Outputs)
            .Where(x => x.StarredDiscordMessageId == messageId)
            .SingleOrDefaultAsync(cancellationToken: cancellationToken);

        if (entry is not null)
        {
            if (numberOfReactions >= numberOfReactionsRequired)
            {
                await HandleExistingStarboardEntry(entry, numberOfReactions, cancellationToken);
            }
            else
            {
                await DeleteStarboardEntry(entry, cancellationToken);
            }
        }
        else if (numberOfReactions >= numberOfReactionsRequired)
        {
            await HandleNewStarboardEntry(messageId, channelId, parentChannelId, userId, numberOfReactions, cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task MessageUpdateForStarboard(ulong messageId,
        ulong channelId,
        ulong userId,
        CancellationToken cancellationToken)
    {
        var hasStarboardEntry = await db.StarboardEntries
            .Where(x => x.StarredDiscordMessageId == messageId)
            .AnyAsync(cancellationToken);

        if (!hasStarboardEntry)
            return;
        
        var channel = await mediator.Send(new GetDiscordGuildChannelRequest(channelId), cancellationToken);

        if (!channel.Success)
            return;

        await HandleMessageForStarboard(messageId,
            channelId,
            channel.Value!.ParentDiscordChannelId,
            userId,
            cancellationToken);
    }

    private async Task HandleNewStarboardEntry(ulong messageId, 
        ulong channelId,
        ulong? parentChannelId,
        ulong userId,
        int numberOfReactions,
        CancellationToken cancellationToken)
    {
        var channelIds = await GetStarboardChannelIdsToPostIn(channelId, parentChannelId, cancellationToken);

        if (!channelIds.Any())
            return;

        var entry = new StarboardEntry
        {
            StarredDiscordMessageId = messageId,
            StarredDiscordMessageChannelId = channelId,
            StarredDiscordUserId = userId,
            Stars = numberOfReactions
        };

        foreach (var channel in channelIds)
        {
            var discordStarboardMessageId = await mediator
                .Send(new RelayNewStarboardEntryToDiscordRequest(channel,
                        messageId,
                        channelId,
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

        db.StarboardEntries.Add(entry);
    }

    private async Task<IReadOnlyCollection<ulong>> GetStarboardChannelIdsToPostIn(ulong channelId, ulong? parentChannelId, CancellationToken cancellationToken)
    {
        if (parentChannelId is not null)
        {
            var hasParentDesignatedStarboards = await db.StarboardChannels
                .Where(x => x.DiscordChannelBeingStarredInId == parentChannelId)
                .AnyAsync(cancellationToken);

            if (hasParentDesignatedStarboards)
            {
                return await db.StarboardChannels
                    .Where(x => x.DiscordChannelBeingStarredInId == parentChannelId)
                    .Select(x => x.DiscordStarboardChannelId)
                    .ToListAsync(cancellationToken: cancellationToken);
            }
        }

        var hasDesignatedStarboards = await db.StarboardChannels
            .Where(x => x.DiscordChannelBeingStarredInId == channelId)
            .AnyAsync(cancellationToken);

        return hasDesignatedStarboards
            ? await db.StarboardChannels
                .Where(x => x.DiscordChannelBeingStarredInId == channelId)
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
            await mediator
                .Send(new RelayExistingStarboardEntryToDiscordRequest(output.DiscordMessageId,
                        output.DiscordChannelId,
                        entry.StarredDiscordMessageId,
                        entry.StarredDiscordMessageChannelId,
                        GetEmojiForNumberOfReactions(numberOfReactions),
                        numberOfReactions),
                    cancellationToken);
        }

        entry.Stars = numberOfReactions;
    }

    private static string GetEmojiForNumberOfReactions(int numberOfReactions)
    {
        return StarboardConstants.WeightedStars
            .Where(x => x.Key <= numberOfReactions)
            .MaxBy(x => x.Key)
            .Value;
    }
    
    internal async Task DeleteStarboardEntriesForMessage(ulong discordMessageId, CancellationToken cancellationToken)
    {
        var starboardEntries = await db.StarboardEntries
            .Include(x => x.Outputs)
            .Where(x => x.StarredDiscordMessageId == discordMessageId)
            .ToListAsync(cancellationToken);

        foreach (var entry in starboardEntries)
        {
            await DeleteStarboardEntry(entry, cancellationToken);
        }
        
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task DeleteStarboardEntry(StarboardEntry entry, CancellationToken cancellationToken)
    {
        foreach (var output in entry.Outputs)
        {
            try
            {
                await mediator.Send(new DeleteDiscordMessageRequest(output.DiscordMessageId,
                        output.DiscordChannelId),
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
}