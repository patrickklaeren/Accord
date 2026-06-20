using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using Accord.Domain.Model;
using LazyCache;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services.Users;

[RegisterScoped]
public class UserProfileService(AccordContext db, 
    UserService userService, 
    IAppCache appCache)
{
    private const int NUMBER_OF_DAYS_TO_LOOK_BACK = 30;

    public async Task<GetUserProfileDto?> GetProfile(ulong discordUserId, CancellationToken cancellationToken)
    {
        var userExists = await userService.UserExists(discordUserId, cancellationToken);

        return userExists
            ? await GetKnownProfile(discordUserId, cancellationToken)
            : null;
    }
    
    private async Task<GetUserProfileDto> GetKnownProfile(ulong discordUserId, CancellationToken cancellationToken)
    {
        var user = await userService.GetUser(discordUserId, cancellationToken);

        var historySummary = await appCache.GetOrAddAsync(BuildGetHistorySummaryCacheKey(discordUserId),
            () => GetHistorySummary(discordUserId),
            DateTimeOffset.UtcNow.AddMinutes(5));

        var messagesSent = await appCache.GetOrAddAsync(BuildGetMessagesCacheKey(discordUserId),
            () => GetMessages(discordUserId),
            DateTimeOffset.UtcNow.AddMinutes(5));

        var voice = await appCache.GetOrAddAsync(BuildGetVoiceMinutesCacheKey(discordUserId),
            () => GetVoiceMinutes(discordUserId),
            DateTimeOffset.UtcNow.AddMinutes(5));

        return new GetUserProfileDto(user, historySummary, messagesSent, voice);
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

    private async Task<IReadOnlyCollection<UserMessagesInChannelDto>> GetMessages(ulong discordUserId)
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

    private async Task<IReadOnlyCollection<UserVoiceMinutesInChannelDto>> GetVoiceMinutes(ulong discordUserId)
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

public sealed record GetUserProfileDto(
    UserDto User,
    UserHistorySummaryDto HistorySummary,
    IReadOnlyCollection<UserMessagesInChannelDto> Messages,
    IReadOnlyCollection<UserVoiceMinutesInChannelDto> VoiceMinutes);

public sealed record UserMessagesInChannelDto(ulong DiscordChannelId, int NumberOfMessages);
public sealed record UserVoiceMinutesInChannelDto(ulong DiscordChannelId, double NumberOfMinutes);
public sealed record UserHistorySummaryDto(int Notes, int Warnings, int Mutes, int Kicks, int Bans)
{
    public int Total => Notes + Warnings + Mutes + Kicks + Bans;
}