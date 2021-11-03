using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Accord.Services;
using Accord.Services.Users;
using MediatR;
using Remora.Commands.Attributes;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Conditions;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Core;
using Remora.Results;

namespace Accord.Bot.CommandGroups;

[Group("debug")]
public class DebugCommandGroup : AccordCommandGroup
{
    private readonly IMediator _mediator;
    private readonly CommandResponder _commandResponder;
    private readonly IDiscordRestChannelAPI _channelApi;
    private readonly ICommandContext _commandContext;
    private readonly IDiscordRestGuildAPI _guildApi;

    public DebugCommandGroup(IMediator mediator,
        CommandResponder commandResponder,
        IDiscordRestChannelAPI channelApi,
        ICommandContext commandContext,
        IDiscordRestGuildAPI guildApi)
    {
        _mediator = mediator;
        _commandResponder = commandResponder;
        _channelApi = channelApi;
        _commandContext = commandContext;
        _guildApi = guildApi;
    }

    [RequireDiscordPermission(DiscordPermission.Administrator), Command("sync"), Description("Synchronises all users and roles in the guild, long running")]
    public async Task<IResult> SynchroniseGuild()
    {
        var timeStarted = DateTime.Now;

        double userDownloadPercentage = 0;
        double savingPercentage = 0;
        string? lastUpdate = null;

        var embedTemplate = "**User download:** {0:P0}"
                               + Environment.NewLine
                               + "**Saved:** {1:P0}"
                               + Environment.NewLine
                               + Environment.NewLine
                               + "**Last update:** {2}";

        await _commandResponder.Respond("Initialise synchronisation...");

        var progressMessageResult = await _channelApi.CreateMessageAsync(_commandContext.ChannelID, embeds: new[]{ CreateEmbed() });

        if (!progressMessageResult.IsSuccess || progressMessageResult.Entity is null)
        {
            throw new InvalidOperationException("Cannot synchronise with missing progress message");
        }

        var progressMessage = progressMessageResult.Entity;
        
        var userDownloadProgress = new Progress<double>(async message => await ProgressUserDownload(message));
        var savingProgress = new Progress<double>(async message => await ProgressSaving(message));
        var lastUpdateProgress = new Progress<string>(async message => await AnnounceUpdate(message));

        var users = await DownloadUsers(userDownloadProgress, lastUpdateProgress);

        await ProgressUserDownload(1);
        await AnnounceUpdate("Done downloading users! Saving...");

        if (users.Success)
        {
            await _mediator.Send(new UpdateUserRolesRequest(savingProgress, users.Value!));
        }

        await ProgressSaving(1);
        await AnnounceUpdate("Done!");

        Embed CreateEmbed()
        {
            var description = string.Format(embedTemplate!, 
                userDownloadPercentage, 
                savingPercentage, 
                lastUpdate);
            
            return new Embed(Title: "Accord Guild Synchroniser",
                Description: description!,
                Footer: new EmbedFooter($"Started {timeStarted:dd/MM/yyyy HH:mm:ss}"));
        }

        async Task ProgressUserDownload(double progress)
        {
            userDownloadPercentage = progress;
            await UpdateProgressEmbed();
        }

        async Task ProgressSaving(double progress)
        {
            savingPercentage = progress;
            await UpdateProgressEmbed();
        }

        async Task AnnounceUpdate(string message)
        {
            lastUpdate = message;
            await UpdateProgressEmbed();
        }

        async Task UpdateProgressEmbed()
        {
            await _channelApi.EditMessageAsync(progressMessage!.ChannelID, progressMessage.ID, embeds: new[] { CreateEmbed() });
        }

        return Result.FromSuccess();
    }

    private async Task<ServiceResponse<List<UserToRoleDto>>> DownloadUsers(IProgress<double> userDownloadProgress, IProgress<string> lastUpdateProgress)
    {
        // Documented on Discord docs
        const int DEFAULT_MAX = 1000;
        
        Snowflake? afterSnowflake = null;

        var toReturn = new List<UserToRoleDto>();

        var guild = await _guildApi.GetGuildPreviewAsync(_commandContext.GuildID.Value);

        var members = guild.Entity.ApproximateMemberCount;

        lastUpdateProgress.Report($"Downloading users from Discord, discovered {members}...");

        while (true)
        {
            var batch = await _guildApi.ListGuildMembersAsync(_commandContext.GuildID.Value, DEFAULT_MAX, after: afterSnowflake ?? new Optional<Snowflake>());

            if (!batch.IsSuccess)
            {
                return ServiceResponse.Fail<List<UserToRoleDto>>(batch.Error.Message);
            }

            var userToRoles = batch.Entity.Select(x => new UserToRoleDto(x.User.Value.ID.Value, x.Roles.Select(d => d.Value).ToList()));
            toReturn.AddRange(userToRoles);

            afterSnowflake = batch.Entity[^1].User.Value.ID;
            
            userDownloadProgress.Report(toReturn.Count / (double)members.Value);

            if (batch.Entity.Count < DEFAULT_MAX)
                break;
        }

        return ServiceResponse.Ok(toReturn);
    }
}