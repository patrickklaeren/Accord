using System;
using System.ComponentModel;
using System.Linq;
using System.Text;
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

[AutoConstructor]
public partial class ParticipationCommandGroup : AccordCommandGroup
{
    private readonly IMediator _mediator;
    private readonly FeedbackService _feedbackService;

    [Command("leaderboard"), Description("Get a leaderboard of XP")]
    public async Task<IResult> GetLeaderboard()
    {
        var leaderboard = await _mediator.Send(new GetLeaderboardRequest());

        var stringBuilder = new StringBuilder();

        var leaderboardPayload = string.Join(Environment.NewLine, leaderboard.MessageUsers
            .Select((user, position) => $"[{position + 1}] {DiscordFormatter.UserIdToMention(user.DiscordUserId)} {user.ParticipationPoints}"));

        stringBuilder.Append(leaderboardPayload);

        var embed = new Embed(Title: "Leaderboard", Description: leaderboardPayload, Footer: new EmbedFooter("See individual statistics via the /profile command"));

        return await _feedbackService.SendContextualEmbedAsync(embed);
    }

    [RequireDiscordPermission(DiscordPermission.Administrator), Command("calculate-xp"), Description("Calculate XP, long running"), Ephemeral]
    public async Task<IResult> CalculateXp()
    {
        await _mediator.Send(new CalculateParticipationRequest());
        return await _feedbackService.SendContextualAsync("Calculated!");
    }
}