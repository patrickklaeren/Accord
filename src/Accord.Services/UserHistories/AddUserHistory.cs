using System;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using Accord.Domain.Model;
using Accord.Services.Users;
using MediatR;

namespace Accord.Services.UserHistories;

public sealed record AddUserHistoryRequest(ulong TargetDiscordUserId, 
    ulong ActingDiscordUserId, 
    string Content, 
    UserHistoryType Type) 
    : IRequest<ServiceResponse<int>>;

public class AddUserHistoryHandler(AccordContext db,
    IMediator mediator,
    UserService userService) 
    : IRequestHandler<AddUserHistoryRequest, ServiceResponse<int>>
{
    public async Task<ServiceResponse<int>> Handle(AddUserHistoryRequest request, CancellationToken cancellationToken)
    {
        var targetUserExists = await userService.UserExists(request.TargetDiscordUserId, cancellationToken);

        if (!targetUserExists)
            return ServiceResponse.Fail<int>("User does not exist");

        var history = new UserHistory
        {
            Type = request.Type,
            Content = request.Content,
            UserId = request.TargetDiscordUserId,
            AddedByUserId = request.ActingDiscordUserId,
            AddedDateTime = DateTimeOffset.UtcNow,
        };

        db.Add(history);

        await db.SaveChangesAsync(cancellationToken);

        IRequest? relayRequest = request.Type switch
        {
            UserHistoryType.Ban => new RelayBanToDiscordRequest(request.ActingDiscordUserId, request.TargetDiscordUserId, request.Content),
            UserHistoryType.Unban => new RelayUnbanToDiscordRequest(request.ActingDiscordUserId, request.TargetDiscordUserId, request.Content),
            UserHistoryType.Kick => new RelayKickToDiscordRequest(request.ActingDiscordUserId, request.TargetDiscordUserId, request.Content),
            UserHistoryType.Warning => new RelayWarningToDiscordRequest(request.ActingDiscordUserId, request.TargetDiscordUserId, request.Content),
            _ => null,
        };

        if (relayRequest is not null)
        {
            await mediator.Publish(relayRequest, cancellationToken);
        }

        return ServiceResponse.Ok(history.Id);
    }
}
