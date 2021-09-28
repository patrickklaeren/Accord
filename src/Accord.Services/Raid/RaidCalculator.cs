using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Accord.Services.Helpers;
using CoenM.ImageHash;
using CoenM.ImageHash.HashAlgorithms;

namespace Accord.Services.Raid
{
    public class RaidCalculator
    {
        private DateTime? _lastJoin;
        private int _joinsInLastRecordedCooldown;

        private readonly List<AccountCreationRange> _accountCreationDateRanges = new();

        private static readonly TimeSpan AccountCreationRange = TimeSpan.FromHours(2);
        private static readonly TimeSpan JoinCooldown = TimeSpan.FromSeconds(90);
        private static DateTime ARBITRARY_EPOCH = new(2021, 09, 01);
        private const ulong THIS_IS_THE_HASH = 17287036140796347265;
        private const int ARBITRARY_SIMILARITY_FACTORY = 75;

        private readonly IHttpClientFactory _httpClientFactory;

        public RaidCalculator(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<bool> CalculateIsRaid(UserJoin userJoin, int sequentialLimit, int accountCreationSimilarityLimit)
        {
            if (userJoin.AvatarUrl is not null)
            {
                var client = _httpClientFactory.CreateClient();
                await using var imageStream = await client.GetStreamAsync(userJoin.AvatarUrl);
                var hashAlgorithm = new AverageHash();
                var avatarHash = hashAlgorithm.Hash(imageStream!);

                if (CompareHash.Similarity(avatarHash, THIS_IS_THE_HASH) > ARBITRARY_SIMILARITY_FACTORY)
                {
                    return true;
                }
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

            var isAccountCreationRaid = IsAccountCreationRisk(userJoin, accountCreationSimilarityLimit);

            _lastJoin = userJoin.JoinedDateTime;

            return _joinsInLastRecordedCooldown >= sequentialLimit
                   || isAccountCreationRaid;
        }

        private bool IsAccountCreationRisk(UserJoin userJoin, int accountCreationSimilarityLimit)
        {
            var accountCreated = DiscordSnowflakeHelper.ToDateTimeOffset(userJoin.DiscordUserId);

            if(accountCreated < ARBITRARY_EPOCH)
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
}