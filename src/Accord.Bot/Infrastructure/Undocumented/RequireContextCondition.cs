using System;
using System.Threading;
using System.Threading.Tasks;
using Remora.Commands.Conditions;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Commands.Conditions;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Results;
using Remora.Results;
using static Remora.Discord.API.Abstractions.Objects.ChannelType;

namespace Accord.Bot.Infrastructure.Undocumented
{
    /// <summary>
    /// Checks required contexts before allowing execution.
    /// </summary>
    public class RequireContextCondition : ICondition<RequireContextAttribute>
    {
        private readonly ICommandContext _context;
        private readonly IDiscordRestChannelAPI _channelAPI;

        /// <summary>
        /// Initializes a new instance of the <see cref="RequireContextCondition"/> class.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="channelAPI">The channel API.</param>
        public RequireContextCondition
        (
            ICommandContext context,
            IDiscordRestChannelAPI channelAPI
        )
        {
            _context = context;
            _channelAPI = channelAPI;
        }

        /// <inheritdoc />
        public async ValueTask<Result> CheckAsync(RequireContextAttribute attribute, CancellationToken ct)
        {
            var getChannel = await _channelAPI.GetChannelAsync(_context.ChannelID, ct);
            if (!getChannel.IsSuccess)
            {
                return Result.FromError(getChannel);
            }

            var channel = getChannel.Entity;

            return attribute.Context switch
            {
                ChannelContext.DM => channel.Type is DM
                    ? Result.FromSuccess()
                    : new ConditionNotSatisfiedError("This command can only be used in a DM."),
                ChannelContext.GroupDM => channel.Type is GroupDM
                    ? Result.FromSuccess()
                    : new ConditionNotSatisfiedError("This command can only be used in a group DM."),
                ChannelContext.Guild =>
                    channel.Type is GuildText or GuildVoice or GuildCategory or GuildNews or GuildStore or
                        GuildPrivateThread or GuildPublicThread
                        ? Result.FromSuccess()
                        : new ConditionNotSatisfiedError("This command can only be used in a guild."),
                _ => throw new ArgumentOutOfRangeException(nameof(attribute))
            };
        }
    }
}
