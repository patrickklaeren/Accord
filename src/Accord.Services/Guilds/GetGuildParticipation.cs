using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services.Guilds;

public sealed record GetGuildParticipationRequest(DateTimeOffset AsOf) : IRequest<GuildParticipationResponse>;

public sealed record GuildParticipationResponse(
    long MessagesLast7Days,
    long MessagesLast30Days,
    ulong? MostActiveChannelId,
    long MostActiveChannelMessages);

public sealed class GetGuildParticipationHandler(AccordContext db)
    : IRequestHandler<GetGuildParticipationRequest, GuildParticipationResponse>
{
    public async Task<GuildParticipationResponse> Handle(GetGuildParticipationRequest request, CancellationToken cancellationToken)
    {
        var monthStart = request.AsOf.AddDays(-30);
        var weekStart = request.AsOf.AddDays(-7);

        var channels = await db.UserMessages
            .Where(message => message.SentDateTime >= monthStart && message.SentDateTime <= request.AsOf)
            .GroupBy(message => message.DiscordChannelId)
            .Select(group => new
            {
                ChannelId = group.Key,
                MonthCount = group.LongCount(),
                WeekCount = group.LongCount(message => message.SentDateTime >= weekStart)
            })
            .OrderByDescending(channel => channel.MonthCount)
            .ThenBy(channel => channel.ChannelId)
            .ToListAsync(cancellationToken);

        var mostActiveChannel = channels.FirstOrDefault();

        return new GuildParticipationResponse(
            channels.Sum(channel => channel.WeekCount),
            channels.Sum(channel => channel.MonthCount),
            mostActiveChannel?.ChannelId,
            mostActiveChannel?.MonthCount ?? 0);
    }
}
