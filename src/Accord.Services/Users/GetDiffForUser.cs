using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using Accord.Services.Helpers;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services.Users
{
    public sealed record GetDiffForUserRequest(ulong DiscordUserId, string DiscordUsername, string DiscordDiscriminator, string? DiscordNickname) : IRequest<UserDiffResponse>;
    public sealed record UserDiffResponse(bool HasDiff, List<string> Messages);

    public class GetDiffForUserHandler : IRequestHandler<GetDiffForUserRequest, UserDiffResponse>
    {
        private readonly AccordContext _db;

        public GetDiffForUserHandler(AccordContext db)
        {
            _db = db;
        }

        public async Task<UserDiffResponse> Handle(GetDiffForUserRequest request, CancellationToken cancellationToken)
        {
            var diffMessages = new List<string>();

            var user = await _db.Users
                .Where(x => x.Id == request.DiscordUserId)
                .Select(x => new
                {
                    x.Id,
                    x.UsernameWithDiscriminator,
                    x.Nickname
                }).SingleOrDefaultAsync(cancellationToken: cancellationToken);

            if (user is null)
                return new UserDiffResponse(false, diffMessages);

            var handle = DiscordHandleHelper.BuildHandle(request.DiscordUsername, request.DiscordDiscriminator);

            if (user.UsernameWithDiscriminator != handle)
            {
                diffMessages.Add($"Changed handle from {user.UsernameWithDiscriminator} to {handle}");
            }

            if (user.Nickname != request.DiscordNickname)
            {
                if (string.IsNullOrWhiteSpace(user.Nickname))
                {
                    diffMessages.Add($"Set nickname to `{request.DiscordNickname}`");
                }
                else
                {
                    diffMessages.Add($"Changed nickname from {user.Nickname} to {request.DiscordNickname}");
                }
            }

            return new UserDiffResponse(true, diffMessages);
        }
    }
}
