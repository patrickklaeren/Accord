using System;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using Accord.Domain.Model;
using Accord.Services.Permissions;
using Accord.Services.Users;
using MediatR;

namespace Accord.Services.UserHistories;

public sealed record AddUserHistoryRequest(ulong TargetDiscordUserId, 
    PermissionUser ActingDiscordUser, 
    string Content, 
    UserHistoryType Type) 
    : IRequest<ServiceResponse<int>>;

public class AddUserHistoryHandler(AccordContext db,
    IMediator mediator,
    UserService userService,
    UserPermissionService userPermissionService) 
    : IRequestHandler<AddUserHistoryRequest, ServiceResponse<int>>
{
    public async Task<ServiceResponse<int>> Handle(AddUserHistoryRequest request, CancellationToken cancellationToken)
    {
        if (request.ActingDiscordUser is { IsAdministrator: false, IsBotSelf: false }
            && !await userPermissionService.HasPermission(request.ActingDiscordUser, PermissionType.AddHistories))
        {
            return ServiceResponse.Fail<int>("Missing permission");
        }
        
        var targetUserExists = await userService.UserExists(request.TargetDiscordUserId, cancellationToken);

        if (!targetUserExists)
        {
            return ServiceResponse.Fail<int>("User does not exist");   
        }
        
        var history = new UserHistory
        {
            Type = request.Type,
            Content = request.Content,
            UserId = request.TargetDiscordUserId,
            AddedByUserId = request.ActingDiscordUser.DiscordUserId,
            AddedDateTime = DateTimeOffset.UtcNow,
        };

        db.Add(history);

        await db.SaveChangesAsync(cancellationToken);

        INotification? relayRequest = request.Type switch
        {
            UserHistoryType.Ban => new RelayBanToDiscordRequest(request.ActingDiscordUser.DiscordUserId, request.TargetDiscordUserId, request.Content),
            UserHistoryType.Unban => new RelayUnbanToDiscordRequest(request.ActingDiscordUser.DiscordUserId, request.TargetDiscordUserId, request.Content),
            UserHistoryType.Kick => new RelayKickToDiscordRequest(request.ActingDiscordUser.DiscordUserId, request.TargetDiscordUserId, request.Content),
            UserHistoryType.Warning => new RelayWarningToDiscordRequest(request.ActingDiscordUser.DiscordUserId, request.TargetDiscordUserId, request.Content),
            UserHistoryType.Note => new RelayNoteToDiscordRequest(request.ActingDiscordUser.DiscordUserId, request.TargetDiscordUserId, request.Content),
            _ => null,
        };

        if (relayRequest is not null)
        {
            await mediator.Publish(relayRequest, cancellationToken);
        }

        return ServiceResponse.Ok(history.Id);
    }
}
