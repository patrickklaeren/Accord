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

public class GetUserReportHandler :
    RequestHandler<InvalidateGetUserReportRequest>,
    IRequestHandler<GetUserReportByChannelRequest, UserReport?>,
    IRequestHandler<GetUserReportRequest, UserReport?>
{
    private readonly IAppCache _appCache;
    private readonly AccordContext _accordContext;

    public GetUserReportHandler(IAppCache appCache, AccordContext accordContext)
    {
        _appCache = appCache;
        _accordContext = accordContext;
    }

    public async Task<UserReport?> Handle(GetUserReportByChannelRequest request, CancellationToken cancellationToken)
    {
        var userReport = await _appCache.GetOrAddAsync(
            BuildGetUserReportByChannel(request.DiscordChannelId),
            () => GetUserReportByChannel(request.DiscordChannelId, cancellationToken),
            DateTimeOffset.Now.AddDays(30)
        );

        if (userReport != null)
        {
            _appCache.GetOrAdd(
                BuildGetUserReport(userReport.OpenedByUserId),
                () => userReport,
                DateTimeOffset.Now.AddDays(30)
            );
            _appCache.GetOrAdd(
                BuildGetUserReportByChannel(userReport.OutboxDiscordChannelId),
                () => userReport,
                DateTimeOffset.Now.AddDays(30)
            );
        }

        return userReport;
    }

    public async Task<UserReport?> Handle(GetUserReportRequest request, CancellationToken cancellationToken)
    {
        var userReport = await _appCache.GetOrAddAsync(
            BuildGetUserReport(request.DiscordUserId),
            () => GetUserReport(request.DiscordUserId, cancellationToken),
            DateTimeOffset.Now.AddDays(30)
        );

        if (userReport != null)
        {
            _appCache.GetOrAdd(
                BuildGetUserReportByChannel(userReport.InboxDiscordChannelId),
                () => userReport,
                DateTimeOffset.Now.AddDays(30)
            );
            _appCache.GetOrAdd(
                BuildGetUserReportByChannel(userReport.OutboxDiscordChannelId),
                () => userReport,
                DateTimeOffset.Now.AddDays(30)
            );
        }

        return userReport;
    }

    protected override void Handle(InvalidateGetUserReportRequest request)
    {
        _appCache.Remove(BuildGetUserReport(request.DiscordUserId));
        _appCache.Remove(BuildGetUserReportByChannel(request.DiscordInboxChannelId));
        _appCache.Remove(BuildGetUserReportByChannel(request.DiscordOutboxChannelId));
    }

    private Task<UserReport?> GetUserReportByChannel(ulong discordChannelId, CancellationToken cancellationToken = default) =>
        _accordContext.UserReports
            .Where(x => x.InboxDiscordChannelId == discordChannelId || x.OutboxDiscordChannelId == discordChannelId)
            .SingleOrDefaultAsync(cancellationToken)!;

    private Task<UserReport?> GetUserReport(ulong discordUserId, CancellationToken cancellationToken = default) =>
        _accordContext.UserReports
            .Where(x => x.OpenedByUserId == discordUserId && x.ClosedByUserId == null)
            .SingleOrDefaultAsync(cancellationToken)!;

    private static string BuildGetUserReportByChannel(ulong discordChannelId) =>
        $"{nameof(GetUserReportHandler)}/{nameof(GetUserReportByChannel)}/{discordChannelId}";

    private static string BuildGetUserReport(ulong discordUserId) =>
        $"{nameof(GetUserReportHandler)}/{nameof(GetUserReport)}/{discordUserId}";
}