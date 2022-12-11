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

public sealed record UserDto(ulong Id, string? UsernameWithDiscriminator, string? Nickname, DateTimeOffset? JoinedGuildDateTime, DateTimeOffset FirstSeenDateTime, int ParticipationRank, int ParticipationPoints, double ParticipationPercentile);
public sealed record UserMessagesInChannelDto(ulong DiscordChannelId, int NumberOfMessages);
public sealed record UserVoiceMinutesInChannelDto(ulong DiscordChannelId, double NumberOfMinutes);

[AutoConstructor]
public partial class GetUserHandler : IRequestHandler<GetUserRequest, ServiceResponse<GetUserDto>>
{
    private readonly AccordContext _db;
    private readonly IMediator _mediator;
    private readonly IAppCache _appCache;
    private const int NUMBER_OF_DAYS_TO_LOOK_BACK = 30;

    public async Task<ServiceResponse<GetUserDto>> Handle(GetUserRequest request, CancellationToken cancellationToken)
    {
        var userExists = await _mediator.Send(new UserExistsRequest(request.DiscordUserId), cancellationToken);

        if (!userExists)
            return ServiceResponse.Fail<GetUserDto>("User does not exist");

        var user = await _appCache.GetOrAddAsync(BuildGetUserCacheKey(request.DiscordUserId), 
            () => GetUser(request.DiscordUserId), 
            DateTimeOffset.Now.AddMinutes(5));

        var messagesSent = await _appCache.GetOrAddAsync(BuildGetMessagesCacheKey(request.DiscordUserId),
            () => GetMessages(request.DiscordUserId),
            DateTimeOffset.Now.AddMinutes(5));

        var voice = await _appCache.GetOrAddAsync(BuildGetVoiceMinutesCacheKey(request.DiscordUserId),
            () => GetVoiceMinutes(request.DiscordUserId),
            DateTimeOffset.Now.AddMinutes(5));

        return ServiceResponse.Ok(new GetUserDto(user, messagesSent, voice));
    }

    internal static string BuildGetUserCacheKey(ulong discordUserId)
    {
        return $"{nameof(GetUserHandler)}/{nameof(GetUser)}/{discordUserId}";
    }

    private async Task<UserDto> GetUser(ulong discordUserId)
    {
        return await _db.Users
            .Where(x => x.Id == discordUserId)
            .Select(x => new UserDto(x.Id, x.UsernameWithDiscriminator, x.Nickname, 
                x.JoinedGuildDateTime, x.FirstSeenDateTime, x.ParticipationRank, 
                x.ParticipationPoints, x.ParticipationPercentile))
            .SingleAsync();
    }

    private static string BuildGetMessagesCacheKey(ulong discordUserId)
    {
        return $"{nameof(GetUserHandler)}/{nameof(GetMessages)}/{discordUserId}";
    }

    private async Task<List<UserMessagesInChannelDto>> GetMessages(ulong discordUserId)
    {
        var cutOff = DateTimeOffset.Now.AddDays(-NUMBER_OF_DAYS_TO_LOOK_BACK);

        return await _db.UserMessages
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
        var cutOff = DateTimeOffset.Now.AddDays(-NUMBER_OF_DAYS_TO_LOOK_BACK);

        return await _db.VoiceConnections
            .Where(x => x.UserId == discordUserId)
            .Where(x => x.EndDateTime != null)  
            .Where(x => x.MinutesInVoiceChannel != null)
            .Where(x => x.StartDateTime >= cutOff)
            .GroupBy(x => x.DiscordChannelId)
            .Select(x => new UserVoiceMinutesInChannelDto(x.Key, x.Sum(a => a.MinutesInVoiceChannel!.Value)))
            .ToListAsync();
    }
}