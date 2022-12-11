using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Accord.Services.Users;
using MediatR;
using Remora.Commands.Attributes;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Conditions;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Results;

namespace Accord.Bot.CommandGroups;

[Group("github"), AutoConstructor]
public partial class GitHubChallengesCommandGroup : AccordCommandGroup
{
    private readonly FeedbackService _feedbackService;
    private readonly IMediator _mediator;

    [RequireDiscordPermission(DiscordPermission.Administrator), 
        Command("post-challenge"), 
        Description("Posts challenge to the current channel, parsed via its readme from the GitHub repository")]
    public async Task<IResult> PostChallenge(string readmeUrl)
    {
        var challengeResponse = await _mediator.Send(new GetGitHubChallengeRequest(readmeUrl));

        if (challengeResponse.Failure)
        {
            return await _feedbackService.SendContextualAsync(challengeResponse.ErrorMessage);
        }

        var challenge = challengeResponse.Value!;

        var fields = new List<EmbedField>();

        if (!string.IsNullOrWhiteSpace(challenge.Sample))
        {
            fields.Add(new EmbedField("Sample", challenge.Sample));
        }

        if (challenge.ContributedBy is not null)
        {
            var formattedMentions = string.Join(", ", challenge.ContributedBy.Select(DiscordFormatter.UserIdToMention));
            fields.Add(new EmbedField("Contributed by", formattedMentions));
        }

        fields.Add(new("Take part", "Click the title of this embed to go to the repo and clone the code for extra help. Take part in this challenge by meeting the specification, golfing your answer or providing the best performing code by benchmarking it (using Benchmark.NET). **Post solutions to the solutions thread, and discuss anything else in the discussion thread.**"));

        var embed = new Embed(Title: challenge.Name,
            Description: challenge.Description,
            Url: challenge.Link,
            Fields: fields.ToArray());

        return await _feedbackService.SendContextualEmbedAsync(embed);
    }
}