using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using Accord.Domain.Model;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services.Users;

public sealed record UpdateUserRolesRequest(IProgress<double> ProgressReporter, List<UserToRoleDto> UsersToRoles) : IRequest;

public record UserToRoleDto(ulong DiscordUserId, List<ulong> DiscordRoleIds);

public class UpdateUserRolesHandler : AsyncRequestHandler<UpdateUserRolesRequest>
{
    private readonly AccordContext _db;
    private readonly IMediator _mediator;

    public UpdateUserRolesHandler(AccordContext db, IMediator mediator)
    {
        _db = db;
        _mediator = mediator;
    }

    protected override async Task Handle(UpdateUserRolesRequest request, CancellationToken cancellationToken)
    {
        var countDone = 0;
        
        foreach (var (discordUserId, discordRoleIds) in request.UsersToRoles)
        {
            await _mediator.Send(new EnsureUserExistsRequest(discordUserId), cancellationToken);

            var roles = await _db
                .UserRoles
                .Where(x => x.DiscordUserId == discordUserId)
                .ToListAsync(cancellationToken);

            // Remove any roles where they no longer exist
            foreach (var role in roles.Where(role => !discordRoleIds.Contains(role.DiscordRoleId)))
            {
                _db.UserRoles.Remove(role);
            }

            // Add any roles that are in Discord but not in Accord's database
            foreach (var nonExistentRole in discordRoleIds.Where(d => !roles.Select(q => q.DiscordRoleId).Contains(d)))
            {
                _db.UserRoles.Add(new UserRole(discordUserId, nonExistentRole));
            }

            await _db.SaveChangesAsync(cancellationToken);
            
            request.ProgressReporter.Report(++countDone / (double)request.UsersToRoles.Count);
        }
    }
}