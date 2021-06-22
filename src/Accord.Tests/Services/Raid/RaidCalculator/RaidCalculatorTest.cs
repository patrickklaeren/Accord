using System;
using Accord.Services.Raid;
using Bogus;
using FluentAssertions;
using Moq;
using Xunit;

namespace Accord.Tests.Services.Raid.RaidCalculator
{
    public class CalculateIsRaidTest
    {
        private static readonly (ulong id, DateTime createdDate) NoxSnowflake = (148664936170651648, new DateTime(2016, 02, 15, 05, 41, 22));
        private static readonly (ulong id, DateTime createdDate) PatrickSnowflake = (104975006542372864, new DateTime(2015, 10, 17, 17, 13, 11));
        private static readonly (ulong id, DateTime createdDate) ReacherSnowflake = (328690877725933589, new DateTime(2017, 06, 26, 01, 20, 12));

        private static readonly (ulong id, DateTime createdDate) SimilarCreatedSnowflake1 = (855206764734709791, new DateTime(2021, 06, 17, 23, 06, 21));
        private static readonly (ulong id, DateTime createdDate) SimilarCreatedSnowflake2 = (855207049863495710, new DateTime(2021, 06, 17, 23, 07, 29));
        private static readonly (ulong id, DateTime createdDate) SimilarCreatedSnowflake3 = (855207049863495710, new DateTime(2021, 06, 17, 23, 08, 29));

        [Fact]
        public void WhenLimitIsExceeded_ShouldDetectRaid()
        {
            var sut = new Accord.Services.Raid.RaidCalculator();

            const int LIMIT = 3;

            var joins = new UserJoin[]
            {
                new(NoxSnowflake.id, new DateTime(2020, 01, 01, 12, 0, 0)),
                new(PatrickSnowflake.id, new DateTime(2020, 01, 01, 12, 0, 0)),
                new(ReacherSnowflake.id, new DateTime(2020, 01, 01, 12, 0, 0)),
            };

            var isRaid = false;

            foreach (var join in joins)
            {
                isRaid = sut.CalculateIsRaid(join, LIMIT);
            }

            isRaid.Should().BeTrue();
        }

        [Fact]
        public void WhenLimitIsNotExceeded_ShouldNotDetectRaid()
        {
            var sut = new Accord.Services.Raid.RaidCalculator();

            const int LIMIT = 3;

            var joins = new UserJoin[]
            {
                new(NoxSnowflake.id, new DateTime(2020, 01, 01, 12, 0, 0)),
                new(PatrickSnowflake.id, new DateTime(2020, 01, 01, 13, 0, 0)),
                new(ReacherSnowflake.id, new DateTime(2020, 01, 01, 14, 0, 0)),
            };

            var isRaid = false;

            foreach (var join in joins)
            {
                isRaid = sut.CalculateIsRaid(join, LIMIT);
            }

            isRaid.Should().BeFalse();
        }

        [Fact]
        public void WhenAccountCreationIsSimilarAndJoinAtTheSameTime_ShouldDetectRaid()
        {
            var sut = new Accord.Services.Raid.RaidCalculator();

            const int LIMIT = 3;

            var joins = new UserJoin[]
            {
                new(SimilarCreatedSnowflake1.id, new DateTime(2020, 01, 01, 12, 0, 0)),
                new(SimilarCreatedSnowflake2.id, new DateTime(2020, 01, 01, 12, 1, 0)),
                new(SimilarCreatedSnowflake3.id, new DateTime(2020, 01, 01, 12, 2, 0)),
            };

            var isRaid = false;

            foreach (var join in joins)
            {
                isRaid = sut.CalculateIsRaid(join, LIMIT);
            }

            isRaid.Should().BeTrue();
        }

        [Fact]
        public void WhenAccountCreationIsSimilarAndDoNotJoinAtTheSameTime_ShouldDetectRaid()
        {
            var sut = new Accord.Services.Raid.RaidCalculator();

            const int LIMIT = 3;

            var joins = new UserJoin[]
            {
                new(SimilarCreatedSnowflake1.id, new DateTime(2020, 01, 01, 12, 0, 0)),
                new(SimilarCreatedSnowflake2.id, new DateTime(2020, 01, 01, 12, 5, 0)),
                new(SimilarCreatedSnowflake3.id, new DateTime(2020, 01, 01, 12, 10, 0)),
            };

            var isRaid = false;

            foreach (var join in joins)
            {
                isRaid = sut.CalculateIsRaid(join, LIMIT);
            }

            isRaid.Should().BeFalse();
        }
    }
}
