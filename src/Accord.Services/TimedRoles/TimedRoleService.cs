using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using Accord.Domain.Model;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Accord.Services.TimedRoles;

[RegisterScoped]
public class TimedRoleService(AccordContext db, IMediator mediator, ILogger<TimedRoleService> logger)
{
    private static readonly string[] Keywords =
    [
        "abstract",
        "as",
        "base",
        "bool",
        "break",
        "byte",
        "case",
        "catch",
        "char",
        "checked",
        "class",
        "const",
        "continue",
        "decimal",
        "default",
        "delegate",
        "do",
        "double",
        "else",
        "enum",
        "event",
        "explicit",
        "extern",
        "false",
        "finally",
        "fixed",
        "float",
        "for",
        "foreach",
        "goto",
        "if",
        "implicit",
        "in",
        "int",
        "interface",
        "internal",
        "is",
        "lock",
        "long",
        "namespace",
        "new",
        "null",
        "object",
        "operator",
        "out",
        "override",
        "params",
        "private",
        "protected",
        "public",
        "readonly",
        "ref",
        "return",
        "sbyte",
        "sealed",
        "short",
        "sizeof",
        "stackalloc",
        "static",
        "string",
        "struct",
        "switch",
        "this",
        "throw",
        "true",
        "try",
        "typeof",
        "uint",
        "ulong",
        "unchecked",
        "unsafe",
        "ushort",
        "using",
        "virtual",
        "void",
        "volatile",
        "while",
    ];

    private Task<bool> CanRequest(ulong roleId, CancellationToken ct) => db.RolePermissions
        .AnyAsync(x => x.RoleId == roleId && x.Type == PermissionType.CanBeRequested, ct);

    public async Task<ServiceResponse<RoleChallenge>> StartChallenge(ulong discordRoleId, CancellationToken ct)
    {
        if (!await CanRequest(discordRoleId, ct))
            return ServiceResponse.Fail<RoleChallenge>("This role cannot be requested.");

        var keyWord = Keywords[Random.Shared.Next(Keywords.Length)];

        return ServiceResponse.Ok(new RoleChallenge(discordRoleId, keyWord));
    }

    public async Task<ServiceResponse> CompleteChallenge(ulong discordUserId, ulong discordRoleId,
        string keyword, string answer, CancellationToken ct)
    {
        if (!Keywords.Contains(keyword)
         || !string.Equals(answer, keyword, StringComparison.Ordinal))
            return ServiceResponse.Fail("Incorrect keyword. Run /get-role again.");

        if (!await CanRequest(discordRoleId, ct))
            return ServiceResponse.Fail("This role can no longer be requested.");

        if (await db.TimedRoles.AnyAsync(x => x.DiscordUserId == discordUserId && x.DiscordRoleId == discordRoleId, ct))
            return ServiceResponse.Fail("You already have a grant for this role.");

        var check = await mediator.Send(new CheckRequestedDiscordRoleRequest(discordUserId, discordRoleId), ct);

        if (check.Failure)
            return check;

        var role = new TimedRole
        {
            DiscordUserId = discordUserId,
            DiscordRoleId = discordRoleId,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(30)
        };

        db.TimedRoles.Add(role);
        await db.SaveChangesAsync(ct);

        var response = await mediator.Send(new GrantRequestedDiscordRoleRequest(role.DiscordUserId, role.DiscordRoleId), ct);

        if (response.Failure)
        {
            db.TimedRoles.Remove(role);
        }
        else
        {
            role.ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(30);
        }
        
        await db.SaveChangesAsync(ct);
        return response;
    }

    public async Task RevokeExpiredRoles(CancellationToken ct)
    {
        var roles = await db
        .TimedRoles
        .Where(x => x.ExpiresAt <= DateTimeOffset.UtcNow)
        .ToListAsync(ct);

        foreach (var role in roles)
        {
            try
            {
                await mediator.Send(new RevokeRequestedDiscordRoleRequest(role.DiscordUserId, role.DiscordRoleId), ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed revoking expired role {RoleId} from {UserId}; deleting grant record.", role.DiscordRoleId, role.DiscordUserId);
            }

            db.TimedRoles.Remove(role);
            await db.SaveChangesAsync(ct);
        }
    }
}
