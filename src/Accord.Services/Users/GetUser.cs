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

public sealed record GetUserRequest(ulong DiscordUserId) : IRequest<ServiceResponse<GetUserDto>>;
public sealed record GetUserDto(UserDto User, UserHistorySummaryDto HistorySummary, List<UserMessagesInChannelDto> Messages, List<UserVoiceMinutesInChannelDto> VoiceMinutes);

public sealed record UserMessagesInChannelDto(ulong DiscordChannelId, int NumberOfMessages);
public sealed record UserVoiceMinutesInChannelDto(ulong DiscordChannelId, double NumberOfMinutes);

public sealed record UserHistorySummaryDto(int Notes, int Warnings, int Mutes, int Kicks, int Bans)
{
    public int Total => Notes + Warnings + Mutes + Kicks + Bans;
};

public class GetUserHandler(AccordContext db, UserService userService, IAppCache appCache) : IRequestHandler<GetUserRequest, ServiceResponse<GetUserDto>>
{
    private const int NUMBER_OF_DAYS_TO_LOOK_BACK = 30;

    public async Task<ServiceResponse<GetUserDto>> Handle(GetUserRequest request, CancellationToken cancellationToken)
    {
        var userExists = await userService.UserExists(request.DiscordUserId, cancellationToken);

        if (!userExists)
            return ServiceResponse.Fail<GetUserDto>("User does not exist");

        var user = await userService.GetUser(request.DiscordUserId, cancellationToken);

        var historySummary = await appCache.GetOrAddAsync(BuildGetHistorySummaryCacheKey(request.DiscordUserId),
            () => GetHistorySummary(request.DiscordUserId),
            DateTimeOffset.UtcNow.AddMinutes(5));

        var messagesSent = await appCache.GetOrAddAsync(BuildGetMessagesCacheKey(request.DiscordUserId),
            () => GetMessages(request.DiscordUserId),
            DateTimeOffset.UtcNow.AddMinutes(5));

        var voice = await appCache.GetOrAddAsync(BuildGetVoiceMinutesCacheKey(request.DiscordUserId),
            () => GetVoiceMinutes(request.DiscordUserId),
            DateTimeOffset.UtcNow.AddMinutes(5));

        return ServiceResponse.Ok(new GetUserDto(user, historySummary, messagesSent, voice));
    }

    private static string BuildGetHistorySummaryCacheKey(ulong discordUserId)
    {
        return $"{nameof(GetUserHandler)}/{nameof(GetHistorySummary)}/{discordUserId}";
    }

    private async Task<UserHistorySummaryDto> GetHistorySummary(ulong discordUserId)
    {
        var histories = await db.UserHistories
            .Where(x => x.UserId == discordUserId)
            .Where(x => x.Type == UserHistoryType.Ban 
                        || x.Type == UserHistoryType.Kick 
                        || x.Type == UserHistoryType.Mute 
                        || x.Type == UserHistoryType.Warning 
                        || x.Type == UserHistoryType.Note)
            .Select(x => x.Type)
            .ToListAsync();
        
        return new UserHistorySummaryDto(
            histories.Count(d => d == UserHistoryType.Note),
            histories.Count(d => d == UserHistoryType.Warning),
            histories.Count(d => d == UserHistoryType.Mute),
            histories.Count(d => d == UserHistoryType.Kick),
            histories.Count(d => d == UserHistoryType.Ban)
        );
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