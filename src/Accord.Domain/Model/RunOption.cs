using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Accord.Domain.Model;

public class RunOption
{
    public RunOptionType Type { get; set; }
    public string Value { get; set; } = null!;
}

public class RunOptionEntityTypeConfiguration : IEntityTypeConfiguration<RunOption>
{
    public void Configure(EntityTypeBuilder<RunOption> builder)
    {
        builder
            .HasKey(x => x.Type);

        builder.HasData(new RunOption()
            {
                Type = RunOptionType.RaidModeEnabled,
                Value = "False"
            }, new RunOption()
            {
                Type = RunOptionType.AutoRaidModeEnabled,
                Value = "False"
            }, new RunOption()
            {
                Type = RunOptionType.SequentialJoinsToTriggerRaidMode,
                Value = "10"
            }, new RunOption()
            {
                Type = RunOptionType.UserReportsEnabled,
                Value = "False"
            }, new RunOption()
            {
                Type = RunOptionType.UserReportsOutboxCategoryId,
                Value = ""
            }, new RunOption()
            {
                Type = RunOptionType.UserReportsInboxCategoryId,
                Value = ""
            }, new RunOption()
            {
                Type = RunOptionType.UserReportsAgentRoleId,
                Value = ""
            }, new RunOption()
            {
                Type = RunOptionType.UserHiddenChannelsCascadeHideEnabled,
                Value = "False"
            }, new RunOption()
            {
                Type = RunOptionType.AccountCreationSimilarityJoinsToTriggerRaidMode,
                Value = "3"
            }
        );
    }
}

public enum RunOptionType
{
    RaidModeEnabled = 0,
    AutoRaidModeEnabled = 1,
    SequentialJoinsToTriggerRaidMode = 2,

    UserReportsEnabled = 3,
    UserReportsOutboxCategoryId = 4,
    UserReportsInboxCategoryId = 5,
    UserReportsAgentRoleId = 6,

    UserHiddenChannelsCascadeHideEnabled = 8,
        
    AccountCreationSimilarityJoinsToTriggerRaidMode = 7,
}