using System.Threading;
using System.Threading.Tasks;
using Accord.Services.Moderation;
using MediatR;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Rest.Core;
using Remora.Discord.Rest.API;

namespace Accord.Bot.RequestHandlers;

public class KickHandler(IDiscordRestGuildAPI guildApi) : IRequestHandler<KickRequest>
{
    public async Task Handle(KickRequest request, CancellationToken cancellationToken)
    {
        using (_ = ((DiscordRestGuildAPI)guildApi).WithCustomization(r => r.AddHeader("X-Audit-Log-Reason", request.Reason)))
        {
            await guildApi.RemoveGuildMemberAsync(new Snowflake(request.DiscordGuildId), new Snowflake(request.DiscordUserId), ct: cancellationToken);
        }
    }
}