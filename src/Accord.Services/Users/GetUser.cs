using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using Accord.Domain.Model;
using LazyCache;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services.Users;

public sealed record GetUserRequest(ulong DiscordUserId) : IRequest<ServiceResponse<UserDto>>;

public class GetUserHandler(UserService userService) 
    : IRequestHandler<GetUserRequest, ServiceResponse<UserDto>>
{
    public async Task<ServiceResponse<UserDto>> Handle(GetUserRequest request, CancellationToken cancellationToken)
    {
        var userExists = await userService.UserExists(request.DiscordUserId, cancellationToken);

        if (!userExists)
        {
            return ServiceResponse.Fail<UserDto>("User does not exist or has not been tracked");
        }
        
        var user = await userService.GetUser(request.DiscordUserId, cancellationToken);
        return ServiceResponse.Ok(user);
    }
}