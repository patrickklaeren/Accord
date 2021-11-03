using System.Threading;
using System.Threading.Tasks;
using Accord.Services.Participation;
using MediatR;
using Remora.Discord.API.Abstractions.Rest;

namespace Accord.Bot.RequestHandlers;

public class UpdateDiscordParticipationRoleHandler : AsyncRequestHandler<UpdateDiscordParticipationRoleRequest>
{
    private readonly IDiscordRestGuildAPI _guildApi;

    public UpdateDiscordParticipationRoleHandler(IDiscordRestGuildAPI guildApi)
    {
        _guildApi = guildApi;
    }

    protected override Task Handle(UpdateDiscordParticipationRoleRequest request, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}