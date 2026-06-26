using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain.Model;
using Accord.Services.Permissions;
using Accord.Services.RunOptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Accord.Services.Spam;

public sealed record AddSpamCheckMessageRequest(
    ulong DiscordMessageId,
    ulong DiscordMessageChannelId,
    string Content,
    PermissionUser User
) : IRequest;

internal class AddSpamCheckMessageHandler(ILogger<AddSpamCheckMessageHandler> logger,
    RunOptionService runOptionService,
    SpamAnalysisService spamAnalysis,
    UserPermissionService userPermissionService,
    IMediator mediator)
    : IRequestHandler<AddSpamCheckMessageRequest>
{
    public async Task Handle(AddSpamCheckMessageRequest request, CancellationToken cancellationToken)
    {
        var enableSpamMute = await runOptionService.GetOption<bool>(RunOptionKey.SpamMuteEnabled);

        if (!enableSpamMute)
        {
            return;
        }
        
        // if (await userPermissionService.HasPermission(request.User, PermissionType.BypassSpamCheck))
        // {
        //     return;
        // }

        var spamMessageThreshold = await runOptionService.GetOption<int>(RunOptionKey.SpamMessageThreshold);
        var spamMessageWindowInSeconds = await runOptionService.GetOption<int>(RunOptionKey.SpamMessageWindowInSeconds);

        var matches = spamAnalysis.TryGetSpamMatches(request.User.DiscordUserId,
            request.DiscordMessageId,
            request.DiscordMessageChannelId,
            request.Content,
            spamMessageThreshold,
            spamMessageWindowInSeconds);

        if (matches.Count > 0)
        {
            var timeoutInSeconds = await runOptionService.GetOption<int>(RunOptionKey.SpamTimeoutInSeconds);
            
            var allMessages = matches
                .Append(new SpamMatch(request.DiscordMessageId, request.DiscordMessageChannelId))
                .ToList();

            await mediator.Publish(new CleanUpSpamRequest(request.User.DiscordUserId, allMessages, timeoutInSeconds), cancellationToken);
        }
    }
}
