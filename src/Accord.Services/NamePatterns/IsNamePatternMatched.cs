using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain.Model;
using Accord.Services.Helpers;
using Accord.Services.Moderation;
using Accord.Services.Raid;
using MediatR;

namespace Accord.Services.NamePatterns
{
    public sealed record ScanNameForPatternsRequest(ulong DiscordGuildId, GuildUserDto User) : IRequest;

    public sealed record NamePatternAlertRequest(ulong DiscordGuildId, GuildUserDto User, string MatchedOnPattern) : IRequest;
    public sealed record NamePatternKickRequest(ulong DiscordGuildId, GuildUserDto User, string MatchedOnPattern) : IRequest;
    public sealed record NamePatternBanRequest(ulong DiscordGuildId, GuildUserDto User, string MatchedOnPattern) : IRequest;

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

                var userHandle = DiscordHandleHelper.BuildHandle(request.User.Username, request.User.Discriminator);

                if (pattern.IsMatch(userHandle))
                {
                    isBlockedMatch = true;
                    matchedBlockedContent = userHandle;
                    matchedOnPattern = pattern.ToString();
                }

                if (!string.IsNullOrWhiteSpace(request.User.DiscordNickname) 
                    && pattern.IsMatch(request.User.DiscordNickname))
                {
                    isBlockedMatch = true;
                    matchedBlockedContent = request.User.DiscordNickname;
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
                        OnNamePatternDiscovery.Alert => new NamePatternAlertRequest(request.DiscordGuildId, request.User, matchedOnPattern!),
                        OnNamePatternDiscovery.Kick => new KickRequest(request.DiscordGuildId, request.User, $"Name {matchedBlockedContent!} matches banned pattern {matchedOnPattern!}"),
                        OnNamePatternDiscovery.Ban => new BanRequest(request.DiscordGuildId, request.User, $"Name {matchedBlockedContent!} matches banned pattern {matchedOnPattern!}"),
                        _ => throw new ArgumentOutOfRangeException()
                    };

                    await _mediator.Send(message, cancellationToken);
                }
            }
        }
    }
}
