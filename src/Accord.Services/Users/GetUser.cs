using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using LazyCache;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services.Users;

public sealed record GetUserRequest(ulong DiscordUserId) : IRequest<ServiceResponse<GetUserDto>>;
public sealed record GetUserDto(UserDto User, List<UserMessagesInChannelDto> Messages, List<UserVoiceMinutesInChannelDto> VoiceMinutes);

public sealed record UserMessagesInChannelDto(ulong DiscordChannelId, int NumberOfMessages);
public sealed record UserVoiceMinutesInChannelDto(ulong DiscordChannelId, double NumberOfMinutes);

public class GetUserHandler(AccordContext db, UserService userService, IAppCache appCache) : IRequestHandler<GetUserRequest, ServiceResponse<GetUserDto>>
{
    private const int NUMBER_OF_DAYS_TO_LOOK_BACK = 30;

    public async Task<ServiceResponse<GetUserDto>> Handle(GetUserRequest request, CancellationToken cancellationToken)
    {
        var userExists = await userService.UserExists(request.DiscordUserId, cancellationToken);

        if (!userExists)
            return ServiceResponse.Fail<GetUserDto>("User does not exist");

        var user = await userService.GetUser(request.DiscordUserId, cancellationToken);

        var messagesSent = await appCache.GetOrAddAsync(BuildGetMessagesCacheKey(request.DiscordUserId),
            () => GetMessages(request.DiscordUserId),
            DateTimeOffset.UtcNow.AddMinutes(5));

        var voice = await appCache.GetOrAddAsync(BuildGetVoiceMinutesCacheKey(request.DiscordUserId),
            () => GetVoiceMinutes(request.DiscordUserId),
            DateTimeOffset.UtcNow.AddMinutes(5));

        return ServiceResponse.Ok(new GetUserDto(user, messagesSent, voice));
    }

    private static string BuildGetMessagesCacheKey(ulong discordUserId)
    {
        return $"{nameof(GetUserHandler)}/{nameof(GetMessages)}/{discordUserId}";
    }

    private async Task<List<UserMessagesInChannelDto>> GetMessages(ulong discordUserId)
    {
        var cutOff = DateTimeOffset.UtcNow.AddDays(-NUMBER_OF_DAYS_TO_LOOK_BACK);

        return await db.UserMessages
            .Where(x => x.UserId == discordUserId)
            .Where(x => x.SentDateTime >= cutOff)
            .GroupBy(x => x.DiscordChannelId)
            .Select(x => new UserMessagesInChannelDto(x.Key, x.Count()))
            .ToListAsync();
    }

    private static string BuildGetVoiceMinutesCacheKey(ulong discordUserId)
    {
        return $"{nameof(GetUserHandler)}/{nameof(GetVoiceMinutes)}/{discordUserId}";
    }

    private async Task<List<UserVoiceMinutesInChannelDto>> GetVoiceMinutes(ulong discordUserId)
    {
        var cutOff = DateTimeOffset.UtcNow.AddDays(-NUMBER_OF_DAYS_TO_LOOK_BACK);

        return await db.VoiceConnections
            .Where(x => x.UserId == discordUserId)
            .Where(x => x.EndDateTime != null)  
            .Where(x => x.MinutesInVoiceChannel != null)
            .Where(x => x.StartDateTime >= cutOff)
            .GroupBy(x => x.DiscordChannelId)
            .Select(x => new UserVoiceMinutesInChannelDto(x.Key, x.Sum(a => a.MinutesInVoiceChannel!.Value)))
            .ToListAsync();
    }
}