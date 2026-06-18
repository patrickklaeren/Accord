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

public sealed record GetUserReportMessageRequest(ulong DiscordMessageId) : IRequest<UserReportMessage?>;

public sealed record InvalidateGetUserReportMessageRequest(ulong DiscordMessageId) : IRequest;

public class GetUserReportMessageHandler(AccordContext accordContext, IAppCache appCache) :
    IRequestHandler<InvalidateGetUserReportMessageRequest>,
    IRequestHandler<GetUserReportMessageRequest, UserReportMessage?>
{

    public async Task<UserReportMessage?> Handle(GetUserReportMessageRequest request, CancellationToken cancellationToken) =>
        await appCache.GetOrAdd(
            BuildGetUserReportMessage(request.DiscordMessageId),
            () => GetUserReportMessage(request.DiscordMessageId, cancellationToken),
            DateTimeOffset.UtcNow.AddMinutes(10)
        );

    private Task<UserReportMessage?> GetUserReportMessage(ulong discordMessageId, CancellationToken ctx = default) =>
        accordContext.UserReportMessages
            .Where(x => x.Id == discordMessageId || x.DiscordProxyMessageId == discordMessageId)
            .SingleOrDefaultAsync(ctx)!;

    private string BuildGetUserReportMessage(ulong discordMessageId) =>
        $"{nameof(GetUserReportMessageHandler)}/{nameof(GetUserReportMessage)}/{discordMessageId}";

    public Task Handle(InvalidateGetUserReportMessageRequest request, CancellationToken cancellationToken)
    {
        appCache.Remove(BuildGetUserReportMessage(request.DiscordMessageId));
        return Task.CompletedTask;
    }
}