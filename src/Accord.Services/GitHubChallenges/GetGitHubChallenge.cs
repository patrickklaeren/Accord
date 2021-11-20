using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using MediatR;

namespace Accord.Services.Users;

public sealed record GetGitHubChallengeRequest(string ChallengeReadMeUrl) : IRequest<ServiceResponse<GitHubChallengeDto>>;

public class GetGitHubChallengeHandler : IRequestHandler<GetGitHubChallengeRequest, ServiceResponse<GitHubChallengeDto>>
{
    private readonly IMediator _mediator;
    private readonly HttpClient _httpClient;

    public GetGitHubChallengeHandler(IMediator mediator, HttpClient httpClient)
    {
        _mediator = mediator;
        _httpClient = httpClient;
    }

    public async Task<ServiceResponse<GitHubChallengeDto>> Handle(GetGitHubChallengeRequest request, CancellationToken cancellationToken)
    {
        if(!request.ChallengeReadMeUrl.StartsWith("https://raw.githubusercontent.com/discord-csharp/challenges/main/src/") || !request.ChallengeReadMeUrl.EndsWith("README.md"))
        {
            return ServiceResponse.Fail<GitHubChallengeDto>("Invalid URL");
        }

        var response = await _httpClient.GetAsync("https://raw.githubusercontent.com/discord-csharp/challenges/main/src/Wire%20Ends/README.md", cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return ServiceResponse.Fail<GitHubChallengeDto>($"Failed getting GitHub challenge, with code {response.StatusCode}");
        }

        var rawChallenge = await response.Content.ReadAsStringAsync(cancellationToken);

        var startIndex = rawChallenge.LastIndexOf("Discord meta data. Do not edit. This is used for GitHub => Discord linking.");

        var document = new HtmlDocument();
        document.LoadHtml(rawChallenge[startIndex..]);

        var table = document.DocumentNode.SelectSingleNode("//table")
                    .Descendants("tr")
                    .Where(tr => tr.Elements("td").Count() > 1)
                    .Select(tr => tr.Elements("td").Select(td => td.InnerText.Trim()).ToList())
                    .ToList();

        var challengeName = table[0][1];
        var description = table[1][1];
        var sample = table[2][1];
        var contributedBy = table[3][1]?.Split(',');
        var link = table[4][1];

        var dto = new GitHubChallengeDto(challengeName, description, sample, contributedBy, link);

        return ServiceResponse.Ok(dto);
    }
}

public sealed record GitHubChallengeDto(string Name, string Description, string? Sample, string[]? ContributedBy, string Link);