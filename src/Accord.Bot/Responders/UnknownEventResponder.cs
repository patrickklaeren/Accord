using System.Threading;
using System.Threading.Tasks;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.Gateway.Responders;
using Remora.Results;

namespace Accord.Bot.Responders;

public class UnknownEventResponder : IResponder<IUnknownEvent>
{
    public Task<Result> RespondAsync(IUnknownEvent gatewayEvent, CancellationToken ct = new CancellationToken())
    {
        return Task.FromResult(Result.FromSuccess());
    }
}