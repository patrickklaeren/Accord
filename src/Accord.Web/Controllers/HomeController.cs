using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Services.Guilds;
using Accord.Services.Helpers;
using Accord.Services.Users;
using Accord.Web.DiscordServices;
using Accord.Web.Models;
using Humanizer;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Accord.Web.Controllers;

public sealed class HomeController(IMediator mediator, DiscordUserService currentUser, ILogger<HomeController> logger)
    : AccordController(mediator, currentUser)
{
    [HttpGet("/dashboard")]
    public async Task<IActionResult> Dashboard(CancellationToken ct)
    {
        var userId = CurrentUser.GetDiscordId();
        var model = new DashboardViewModel
        {
            UserId = userId,
            DisplayName = User.Identity?.Name ?? $"User {userId}",
            Handle = User.Identity?.Name ?? $"{userId}",
            AvatarUrl = CurrentUser.GetAvatarUrl(),
            CreatedAt = DiscordSnowflakeHelper.ToDateTimeOffset(userId)
        };

        try
        {
            var member = await Mediator.Send(new GetGuildMemberRequest(userId), ct);
            if (member.Success)
                model = model with
                {
                    DisplayName = member.Value!.Nickname ?? member.Value.Username,
                    Handle = member.Value.Username,
                    Nickname = member.Value.Nickname,
                    IsGuildMember = true
                };

            var profile = await Mediator.Send(new GetUserProfileRequest(userId), ct);
            model = model with { Profile = profile };
            if (profile is not null)
            {
                var topText = profile.Messages.OrderByDescending(channel => channel.NumberOfMessages).FirstOrDefault();
                var topVoice = profile.VoiceMinutes.OrderByDescending(channel => channel.NumberOfMinutes).FirstOrDefault();
                // Channel names are optional; participation is still useful if Discord is unavailable.
                var channels = Array.Empty<GuildChannelDto>();
                if (topText is not null || topVoice is not null)
                {
                    try
                    {
                        channels = (await Mediator.Send(new GetGuildChannelsRequest(), ct)).Channels.ToArray();
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        logger.LogWarning(ex, "Could not resolve dashboard channel names");
                    }
                }

                string ChannelName(ulong id) => channels.FirstOrDefault(channel => channel.Id == id)?.Name ?? $"{id}";
                model = model with
                {
                    MostActiveTextChannel = topText is null ? null : $"#{ChannelName(topText.DiscordChannelId)} ({topText.NumberOfMessages:N0} messages)",
                    MostActiveVoiceChannel = topVoice is null ? null : $"#{ChannelName(topVoice.DiscordChannelId)} ({TimeSpan.FromMinutes(topVoice.NumberOfMinutes).Humanize()})"
                };
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Could not load dashboard for {UserId}", userId);
            model = model with { Error = "We couldn’t load all your profile data. Please try again shortly." };
        }

        return View(model);
    }

    [AllowAnonymous, HttpGet("/")]
    public IActionResult Index() => View();

    [AllowAnonymous, HttpGet("/welcome")]
    public IActionResult Welcome() => RedirectToActionPermanent(nameof(Index));

    [AllowAnonymous, HttpGet("/help")]
    public IActionResult Help() => View(CommandCatalog.Groups);

    [AllowAnonymous, Route("/Error"), IgnoreAntiforgeryToken]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View(new ErrorViewModel(Activity.Current?.Id ?? HttpContext.TraceIdentifier));
}
