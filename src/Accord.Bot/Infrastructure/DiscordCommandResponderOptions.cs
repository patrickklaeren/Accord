using Remora.Discord.Commands.Responders;

namespace Accord.Bot.Infrastructure;

public class DiscordCommandResponderOptions : ICommandResponderOptions
{
    public string? Prefix { get; set; }
}