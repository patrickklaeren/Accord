namespace Accord.Bot.Infrastructure;

public class BotState
{
    public bool IsCacheReady { get; set; }
    public bool IsReady => IsCacheReady;
}