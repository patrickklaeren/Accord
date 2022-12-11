using Accord.Services.Helpers;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;

namespace Accord.Bot.Helpers;

[AutoConstructor, RegisterScoped]
public partial class ThumbnailHelper
{
    private readonly DiscordAvatarHelper _discordAvatarHelper;

    public EmbedThumbnail GetAvatar(IUser user)
    {
        var url = _discordAvatarHelper.GetAvatarUrl(user.ID.Value, 
            user.Discriminator, 
            user.Avatar?.Value, 
            user.Avatar?.HasGif == true);
        
        return new EmbedThumbnail(url);
    }
}