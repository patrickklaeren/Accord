using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Accord.Domain.Model;

public class RunOption
{
    public RunOptionKey Key { get; set; }
    public RunOptionType Type { get; set; }
    public required string Value { get; set; }
}

public class RunOptionEntityTypeConfiguration : IEntityTypeConfiguration<RunOption>
{
    public void Configure(EntityTypeBuilder<RunOption> builder)
    {
        builder
            .HasKey(x => x.Key);

        builder.HasData(new RunOption()
            {
                Key = RunOptionKey.IsInRaidMode,
                Type= RunOptionType.Boolean,
                Value = "False"
            }, new RunOption()
            {
                Key = RunOptionKey.AutoRaidModeEnabled,
                Type= RunOptionType.Boolean,
                Value = "False"
            }, new RunOption()
            {
                Key = RunOptionKey.SequentialJoinsToTriggerRaidMode,
                Type= RunOptionType.Integer,
                Value = "10"
            }, new RunOption()
            {
                Key = RunOptionKey.AccountCreationSimilarityJoinsToTriggerRaidMode,
                Type= RunOptionType.Integer,
                Value = "3"
            }, new RunOption()
            {
                Key = RunOptionKey.StarboardNumberOfReactionsRequired,
                Type= RunOptionType.Integer,
                Value = "3"
            }, new RunOption()
            {
                Key = RunOptionKey.StarboardSelfStarring,
                Type= RunOptionType.Boolean,
                Value = "False"
            }, new RunOption()
            {
                Key = RunOptionKey.SpamMessageWindowInSeconds,
                Type= RunOptionType.Integer,
                Value = "30"
            }, new RunOption()
            {
                Key = RunOptionKey.SpamMessageThreshold,
                Type= RunOptionType.Integer,
                Value = "3"
            }, new RunOption()
            {
                Key = RunOptionKey.SpamMuteEnabled,
                Type= RunOptionType.Boolean,
                Value = "true"
            }, new RunOption()
            {
                Key = RunOptionKey.SpamTimeoutInSeconds,
                Type= RunOptionType.Integer,
                Value = "60"
            }, new RunOption()
            {
                Key = RunOptionKey.VoiceAutoUnmuteEnabled,
                Type= RunOptionType.Boolean,
                Value = "true"
            }, new RunOption()
            {
                Key = RunOptionKey.VoiceAutoUnmuteInMinutes,
                Type= RunOptionType.Integer,
                Value = "1440"
            }, new RunOption()
            {
                Key = RunOptionKey.DemocraticDownVotingEnabled,
                Type= RunOptionType.Boolean,
                Value = "false"
            }, new RunOption()
            {
                Key = RunOptionKey.DemocraticDownVotesRequired,
                Type= RunOptionType.Integer,
                Value = "5"
            }
        );
    }
}

public enum RunOptionKey
{
    IsInRaidMode = 0,
    AutoRaidModeEnabled = 1,
    SequentialJoinsToTriggerRaidMode = 2,
    AccountCreationSimilarityJoinsToTriggerRaidMode = 7,
    StarboardNumberOfReactionsRequired = 8,
    StarboardSelfStarring = 9,
    SpamMessageWindowInSeconds = 10,
    SpamMessageThreshold = 11,
    SpamMuteEnabled = 12,
    SpamTimeoutInSeconds = 13,
    VoiceAutoUnmuteEnabled = 14,
    VoiceAutoUnmuteInMinutes = 15,
    DemocraticDownVotingEnabled = 16,
    DemocraticDownVotesRequired = 17,
}

public enum RunOptionType
{
    String,
    Integer,
    ULong,
    Boolean,
    DateTime,
}