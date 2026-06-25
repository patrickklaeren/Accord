using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using Accord.Domain.Model;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Accord.Services.Starboard;

[RegisterScoped]
internal class StarboardService(IMediator mediator,
    AccordContext db,
    ILogger<StarboardService> logger)
{
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
    
    internal async Task DeleteStarboardEntry(StarboardEntry entry, CancellationToken cancellationToken)
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