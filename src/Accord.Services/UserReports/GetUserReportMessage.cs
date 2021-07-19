using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using Accord.Domain.Model.UserReports;
using LazyCache;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services.UserReports
{
    public sealed record GetUserReportMessageRequest(ulong DiscordMessageId) : IRequest<UserReportMessage?>;

    public sealed record InvalidateGetUserReportMessageRequest(ulong DiscordMessageId) : IRequest;

    public class GetUserReportMessageHandler :
        RequestHandler<InvalidateGetUserReportMessageRequest>,
        IRequestHandler<GetUserReportMessageRequest, UserReportMessage?>
    {
        private readonly AccordContext _accordContext;
        private readonly IAppCache _appCache;

        public GetUserReportMessageHandler(AccordContext accordContext, IAppCache appCache)
        {
            _accordContext = accordContext;
            _appCache = appCache;
        }

        public async Task<UserReportMessage?> Handle(GetUserReportMessageRequest request, CancellationToken cancellationToken) =>
            await _appCache.GetOrAdd(
                BuildGetUserReportMessage(request.DiscordMessageId),
                () => GetUserReportMessage(request.DiscordMessageId, cancellationToken),
                DateTimeOffset.Now.AddMinutes(10)
            );

        protected override void Handle(InvalidateGetUserReportMessageRequest request) => 
            _appCache.Remove(BuildGetUserReportMessage(request.DiscordMessageId));

        private Task<UserReportMessage?> GetUserReportMessage(ulong discordMessageId, CancellationToken ctx = default) =>
            _accordContext.UserReportMessages
                .Where(x => x.Id == discordMessageId || x.DiscordProxyMessageId == discordMessageId)
                .SingleOrDefaultAsync(ctx)!;

        private string BuildGetUserReportMessage(ulong discordMessageId) =>
            $"{nameof(GetUserReportMessageHandler)}/{nameof(GetUserReportMessage)}/{discordMessageId}";
    }
}