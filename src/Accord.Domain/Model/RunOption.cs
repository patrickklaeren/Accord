using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Accord.Domain.Model;

public class RunOption
{
    public RunOptionKey Key { get; set; }
    public RunOptionType Type { get; set; }
    public string Value { get; set; } = null!;
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
                Key = RunOptionKey.NumberOfReactionsForStarboardEntry,
                Type= RunOptionType.Integer,
                Value = "3"
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
    NumberOfReactionsForStarboardEntry = 8,
}
public enum RunOptionType
{
    String,
    Integer,
    ULong,
    Boolean,
    DateTime,
}