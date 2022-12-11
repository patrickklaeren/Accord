using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using LazyCache;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services.UserReports;

public sealed record GetUserReportChannelTypeRequest(ulong DiscordChannelId) : IRequest<UserReportChannelType>;

[AutoConstructor]
public partial class GetUserReportChannelTypeHandler : IRequestHandler<GetUserReportChannelTypeRequest, UserReportChannelType>
{
    private readonly AccordContext _db;
    private readonly IAppCache _appCache;

    public async Task<UserReportChannelType> Handle(GetUserReportChannelTypeRequest channelTypeRequest, CancellationToken cancellationToken)
    {
        return await _appCache.GetOrAddAsync(BuildIsUserReportChannelCacheKey(channelTypeRequest.DiscordChannelId), 
            () => IsUserReportChannel(channelTypeRequest.DiscordChannelId),
            DateTimeOffset.Now.AddDays(30));
    }

    private static string BuildIsUserReportChannelCacheKey(ulong discordChannelId)
    {
        return $"{nameof(GetUserReportChannelTypeHandler)}/{nameof(IsUserReportChannel)}/{discordChannelId}";
    }

    private async Task<UserReportChannelType> IsUserReportChannel(ulong discordChannelId)
    {
        var userReport = await _db.UserReports
            .Where(x => x.OutboxDiscordChannelId == discordChannelId 
                        || x.InboxDiscordChannelId == discordChannelId)
            .Select(x => new
            {
                x.OutboxDiscordChannelId,
                x.InboxDiscordChannelId
            }).FirstOrDefaultAsync();

        if (userReport is null)
            return UserReportChannelType.None;

        if (userReport.InboxDiscordChannelId == discordChannelId)
            return UserReportChannelType.Inbox;

        if (userReport.OutboxDiscordChannelId == discordChannelId)
            return UserReportChannelType.Outbox;

        return UserReportChannelType.None;
    }
}