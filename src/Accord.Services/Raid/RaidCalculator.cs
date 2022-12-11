using System;
using System.Collections.Generic;
using System.Linq;
using Accord.Services.Helpers;

namespace Accord.Services.Raid;

[RegisterSingleton]
public class RaidCalculator
{
    private DateTime? _lastJoin;
    private int _joinsInLastRecordedCooldown;

    private readonly List<AccountCreationRange> _accountCreationDateRanges = new();

    private static readonly TimeSpan AccountCreationRange = TimeSpan.FromHours(2);
    private static readonly TimeSpan JoinCooldown = TimeSpan.FromSeconds(15);
    private static readonly DateTime ArbitraryEpoch = new(2021, 09, 01);

    public RaidResponse CalculateIsRaid(UserJoin userJoin, int sequentialLimit, int accountCreationSimilarityLimit)
    {
        // Check for accounts created in the same range
        if(IsAccountCreationRisk(userJoin, accountCreationSimilarityLimit))
        {
            return new(true, "Account creation");
        }

        if (_lastJoin is null
            || (userJoin.JoinedDateTime - _lastJoin) > JoinCooldown)
        {
            _joinsInLastRecordedCooldown = 1;
        }
        else
        {
            _joinsInLastRecordedCooldown++;
        }

        _lastJoin = userJoin.JoinedDateTime;

        return new(_joinsInLastRecordedCooldown >= sequentialLimit, "Sequential joins exceeded");
    }

    private bool IsAccountCreationRisk(UserJoin userJoin, int accountCreationSimilarityLimit)
    {
        var accountCreated = DiscordSnowflakeHelper.ToDateTimeOffset(userJoin.DiscordUserId);

        if(accountCreated < ArbitraryEpoch)
        {
            return false;
        }

        foreach (var existingRange in _accountCreationDateRanges.Where(x => x.IsExpired()).ToList())
        {
            _accountCreationDateRanges.Remove(existingRange);
        }

        if (_accountCreationDateRanges.Any(x => x.IsRisk(accountCreated.DateTime, accountCreationSimilarityLimit)))
        {
            return true;
        }

        var range = new AccountCreationRange(accountCreated.DateTime.Add(-AccountCreationRange),
            accountCreated.DateTime.Add(AccountCreationRange));

        _accountCreationDateRanges.Add(range);

        return false;
    }
}

public sealed record UserJoin(ulong DiscordUserId, string? AvatarUrl, DateTime JoinedDateTime);
public sealed record RaidResponse(bool IsRaid, string? Reason = null);