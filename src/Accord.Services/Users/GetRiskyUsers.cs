using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using Accord.Domain.Model;
using Accord.Services.Helpers;
using Accord.Services.Permissions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services.Users
{
    public sealed record GetRiskyUsersRequest(PermissionUser User) : IRequest<ServiceResponse<List<RiskyUser>>>;
    public sealed record RiskyUser(ulong DiscordUserId, string? UsernameWithDiscriminator, DateTimeOffset JoinedDateTime, DateTimeOffset CreatedDateTime);

    public class GetRiskyUsersHandler : IRequestHandler<GetRiskyUsersRequest, ServiceResponse<List<RiskyUser>>>
    {
        private readonly AccordContext _db;
        private readonly IMediator _mediator;

        public GetRiskyUsersHandler(AccordContext db, IMediator mediator)
        {
            _db = db;
            _mediator = mediator;
        }

        public async Task<ServiceResponse<List<RiskyUser>>> Handle(GetRiskyUsersRequest request, CancellationToken cancellationToken)
        {
            var hasPermission = await _mediator.Send(new UserHasPermissionRequest(request.User, PermissionType.ListRiskyUsers));

            if (!hasPermission)
            {
                return ServiceResponse.Fail<List<RiskyUser>>("Missing permission");
            }

            var users = await _db.Users
                .Where(x => x.Messages.Any() == false)
                .Where(x => x.JoinedGuildDateTime != null)
                .OrderByDescending(x => x.JoinedGuildDateTime)
                .Take(25)
                .Select(x => new
                {
                    x.Id,
                    x.UsernameWithDiscriminator,
                    JoinedDateTime = x.JoinedGuildDateTime!.Value
                })
                .ToListAsync(cancellationToken: cancellationToken);

            var models = users
                .Select(x => new RiskyUser(x.Id, x.UsernameWithDiscriminator, x.JoinedDateTime, DiscordSnowflakeHelper.ToDateTimeOffset(x.Id)))
                .ToList();

            return ServiceResponse.Ok(models);
        }
    }
}
