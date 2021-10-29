﻿using Accord.Bot.Infrastructure;
using Microsoft.Extensions.Options;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;

namespace Accord.Bot.Helpers;

public class DiscordAvatarHelper
{
    private readonly DiscordConfiguration _discordConfiguration;

    public DiscordAvatarHelper(IOptions<DiscordConfiguration> discordConfiguration)
    {
        _discordConfiguration = discordConfiguration.Value;
    }

    public EmbedThumbnail GetAvatar(IUser user)
    {
        var url = GetAvatarUrl(user);
        return new EmbedThumbnail(url);
    }

    public string GetAvatarUrl(IUser user)
    {
        if (user.Avatar is null)
        {
            var resultModulus = user.Discriminator % 5;
            return $"{_discordConfiguration.CdnBaseUrl}/embed/avatars/{resultModulus}.png";
        }

        var extension = "png";

        if (user.Avatar.HasGif)
        {
            extension = "gif";
        }

        return $"{_discordConfiguration.CdnBaseUrl}/avatars/{user.ID.Value}/{user.Avatar.Value}.{extension}";
    }
}