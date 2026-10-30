using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Accord.Services.Helpers;
using Remora.Commands.Attributes;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Results;

namespace Accord.Bot.CommandGroups;

public sealed class AvatarCommandGroup(
    ICommandContext commandContext,
    DiscordAvatarHelper avatarHelper,
    FeedbackService feedbackService) : AccordCommandGroup
{
    [Command("avatar"), Description("Get a user's avatar")]
    public async Task<IResult> Avatar(
        [Description("User whose avatar to show; defaults to yourself")] IUser? user = null,
        [Description("Image size in pixels; defaults to 128")] AvatarSize size = AvatarSize.Size128)
    {
        if (!Enum.IsDefined(size))
            return await feedbackService.SendContextualAsync("Choose an avatar size of 16, 32, 64, 128, 256, 512, 1024, 2048, or 4096.", ct: CancellationToken);

        user ??= commandContext.GetExecutingUser();
        var avatarUrl = avatarHelper.GetAvatarUrl(
            user.ID.Value, user.Discriminator, user.Avatar?.Value, user.Avatar?.HasGif == true);
        var displayName = user.GlobalName.HasValue && !string.IsNullOrWhiteSpace(user.GlobalName.Value)
            ? user.GlobalName.Value
            : user.Username;

        var embed = new Embed(
            Title: $"{displayName}'s avatar",
            Image: new EmbedImage($"{avatarUrl}?size={(ushort)size}"),
            Timestamp: DateTimeOffset.UtcNow);

        return await feedbackService.SendContextualEmbedAsync(embed, ct: CancellationToken);
    }

    public enum AvatarSize : ushort
    {
        [Description("16")] Size16 = 16,
        [Description("32")] Size32 = 32,
        [Description("64")] Size64 = 64,
        [Description("128")] Size128 = 128,
        [Description("256")] Size256 = 256,
        [Description("512")] Size512 = 512,
        [Description("1024")] Size1024 = 1024,
        [Description("2048")] Size2048 = 2048,
        [Description("4096")] Size4096 = 4096
    }
}
