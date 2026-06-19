using Accord.Services;
using Remora.Discord.API.Abstractions.Objects;

namespace Accord.Bot.Helpers;

[RegisterScoped]
public class JumpLinkHelper(DiscordConfiguration discordConfiguration)
{
    private const string JUMP_URL = "https://discord.com/channels/{0}/{1}/{2}";
    public string FromMessage(IMessage message) => string.Format(JUMP_URL, discordConfiguration.GuildId, message.ChannelID.Value, message.ID.Value);
}