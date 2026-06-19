using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Accord.Services.Participation;
using MediatR;
using Remora.Commands.Attributes;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Conditions;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Results;

namespace Accord.Bot.CommandGroups;

public class ParticipationCommandGroup(IMediator mediator, FeedbackService feedbackService) : AccordCommandGroup
{
    private static readonly string[] Medals = ["🥇", "🥈", "🥉"];

    [Command("leaderboard"), Description("Get a leaderboard of XP")]
    public async Task<IResult> GetLeaderboard()
    {
        var leaderboard = await mediator.Send(new GetLeaderboardRequest());

        var topPoints = leaderboard.MessageUsers.FirstOrDefault()?.ParticipationPoints ?? 1;

        var lines = leaderboard.MessageUsers
            .Select((user, position) =>
            {
                var prefix = position < 3
                    ? Medals[position]
                    : $"`#{position + 1}`";

                var mention = DiscordFormatter.UserIdToMention(user.DiscordUserId);
                var points = $"{user.ParticipationPoints:N0}";

                var barLength = Math.Clamp((int)(user.ParticipationPoints / topPoints * 10), 1, 10);
                var bar = new string('▰', barLength).PadRight(10, '▱');

                return $"{prefix} {mention}\n{bar} **{points}** pts";
            });

        var description = string.Join("\n", lines);

        var embed = new Embed(
            Title: "🏆 Leaderboard",
            Description: description,
            Colour: Color.FromArgb(0xFF, 0xD7, 0x00),
            Footer: new EmbedFooter("See individual statistics via /profile"),
            Timestamp: DateTimeOffset.UtcNow
        );

        return await feedbackService.SendContextualEmbedAsync(embed);
    }

    [RequireDiscordPermission(DiscordPermission.Administrator), Command("calculate-xp"), Description("Calculate XP, long running"), Ephemeral]
    public async Task<IResult> CalculateXp()
    {
        await mediator.Send(new CalculateParticipationRequest());
        return await feedbackService.SendContextualAsync("Calculated!");
    }
}