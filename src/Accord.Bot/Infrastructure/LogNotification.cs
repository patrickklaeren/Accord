using MediatR;
using Serilog.Events;

namespace Accord.Bot.Infrastructure
{
    public sealed record LogNotification(LogEvent LogEvent) : INotification;
}