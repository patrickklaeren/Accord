using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using Accord.Domain.Model.UserReports;
using LazyCache;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services.UserReports;

public sealed record GetUserReportByChannelRequest(ulong DiscordChannelId) : IRequest<UserReport?>;

public sealed record GetUserReportRequest(ulong DiscordUserId) : IRequest<UserReport?>;

public sealed record InvalidateGetUserReportRequest(ulong DiscordUserId, ulong DiscordInboxChannelId, ulong DiscordOutboxChannelId) : IRequest;

public class GetUserReportHandler(IAppCache appCache, AccordContext accordContext) :
    IRequestHandler<InvalidateGetUserReportRequest>,
    IRequestHandler<GetUserReportByChannelRequest, UserReport?>,
    IRequestHandler<GetUserReportRequest, UserReport?>
{

    public async Task<UserReport?> Handle(GetUserReportByChannelRequest request, CancellationToken cancellationToken)
    {
        var userReport = await appCache.GetOrAddAsync(
            BuildGetUserReportByChannel(request.DiscordChannelId),
            () => GetUserReportByChannel(request.DiscordChannelId, cancellationToken),
            DateTimeOffset.UtcNow.AddDays(30)
        );

        if (userReport != null)
        {
            appCache.GetOrAdd(
                BuildGetUserReport(userReport.OpenedByUserId),
                () => userReport,
                DateTimeOffset.UtcNow.AddDays(30)
            );
            appCache.GetOrAdd(
                BuildGetUserReportByChannel(userReport.OutboxDiscordChannelId),
                () => userReport,
                DateTimeOffset.UtcNow.AddDays(30)
            );
        }

        return userReport;
    }

    public async Task<UserReport?> Handle(GetUserReportRequest request, CancellationToken cancellationToken)
    {
        var userReport = await appCache.GetOrAddAsync(
            BuildGetUserReport(request.DiscordUserId),
            () => GetUserReport(request.DiscordUserId, cancellationToken),
            DateTimeOffset.UtcNow.AddDays(30)
        );

        if (userReport != null)
        {
            appCache.GetOrAdd(
                BuildGetUserReportByChannel(userReport.InboxDiscordChannelId),
                () => userReport,
                DateTimeOffset.UtcNow.AddDays(30)
            );
            appCache.GetOrAdd(
                BuildGetUserReportByChannel(userReport.OutboxDiscordChannelId),
                () => userReport,
                DateTimeOffset.UtcNow.AddDays(30)
            );
        }

        return userReport;
    }

    private Task<UserReport?> GetUserReportByChannel(ulong discordChannelId, CancellationToken cancellationToken = default) =>
        accordContext.UserReports
            .Where(x => x.InboxDiscordChannelId == discordChannelId || x.OutboxDiscordChannelId == discordChannelId)
            .SingleOrDefaultAsync(cancellationToken)!;

    private Task<UserReport?> GetUserReport(ulong discordUserId, CancellationToken cancellationToken = default) =>
        accordContext.UserReports
            .Where(x => x.OpenedByUserId == discordUserId && x.ClosedByUserId == null)
            .SingleOrDefaultAsync(cancellationToken)!;

    private static string BuildGetUserReportByChannel(ulong discordChannelId) =>
        $"{nameof(GetUserReportHandler)}/{nameof(GetUserReportByChannel)}/{discordChannelId}";

    private static string BuildGetUserReport(ulong discordUserId) =>
        $"{nameof(GetUserReportHandler)}/{nameof(GetUserReport)}/{discordUserId}";

    public Task Handle(InvalidateGetUserReportRequest request, CancellationToken cancellationToken)
    {
        appCache.Remove(BuildGetUserReport(request.DiscordUserId));
        appCache.Remove(BuildGetUserReportByChannel(request.DiscordInboxChannelId));
        appCache.Remove(BuildGetUserReportByChannel(request.DiscordOutboxChannelId));
        return Task.CompletedTask;
    }
}