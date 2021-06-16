using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain.Model;
using MediatR;

namespace Accord.Services.NamePatterns
{
    public sealed record ScanNameForPatternsRequest(ulong DiscordGuildId, ulong DiscordUserId, string DiscordHandle, string? DiscordNickname) : IRequest;

    public sealed record NamePatternAlertRequest(ulong DiscordGuildId, ulong DiscordUserId, string MatchedOnPattern) : IRequest;
    public sealed record NamePatternKickRequest(ulong DiscordGuildId, ulong DiscordUserId, string MatchedOnPattern) : IRequest;
    public sealed record NamePatternBanRequest(ulong DiscordGuildId, ulong DiscordUserId, string MatchedOnPattern) : IRequest;

    public class ScanNameForPatternsHandler : AsyncRequestHandler<ScanNameForPatternsRequest>
    {
        private readonly IMediator _mediator;

        public ScanNameForPatternsHandler(IMediator mediator)
        {
            _mediator = mediator;
        }

        protected override async Task Handle(ScanNameForPatternsRequest request, CancellationToken cancellationToken)
        {
            var patterns = await _mediator.Send(new GetNamePatternsRequest(), cancellationToken);

            foreach (var (pattern, type, onDiscovery) in patterns.Where(x => x.Type == PatternType.Blocked))
            {
                var isBlockedMatch = false;
                string? matchedBlockedContent = default;
                string? matchedOnPattern = default;

                if (pattern.IsMatch(request.DiscordHandle))
                {
                    isBlockedMatch = true;
                    matchedBlockedContent = request.DiscordHandle;
                    matchedOnPattern = pattern.ToString();
                }

                if (!string.IsNullOrWhiteSpace(request.DiscordNickname) 
                    && pattern.IsMatch(request.DiscordNickname))
                {
                    isBlockedMatch = true;
                    matchedBlockedContent = request.DiscordNickname;
                    matchedOnPattern = pattern.ToString();
                }

                if (isBlockedMatch 
                    && !string.IsNullOrWhiteSpace(matchedBlockedContent) 
                    && patterns.Any(x => x.Type == PatternType.Allowed && x.Pattern.IsMatch(matchedBlockedContent)))
                {
                    continue;
                }

                if (isBlockedMatch && onDiscovery != OnNamePatternDiscovery.DoNothing)
                {
                    IRequest message = onDiscovery switch
                    {
                        OnNamePatternDiscovery.Alert => new NamePatternAlertRequest(request.DiscordGuildId, request.DiscordUserId, matchedOnPattern!),
                        OnNamePatternDiscovery.Kick => new NamePatternKickRequest(request.DiscordGuildId, request.DiscordUserId, matchedOnPattern!),
                        OnNamePatternDiscovery.Ban => new NamePatternBanRequest(request.DiscordGuildId, request.DiscordUserId, matchedOnPattern!),
                        _ => throw new ArgumentOutOfRangeException()
                    };

                    await _mediator.Send(message, cancellationToken);
                }
            }
        }
    }
}
