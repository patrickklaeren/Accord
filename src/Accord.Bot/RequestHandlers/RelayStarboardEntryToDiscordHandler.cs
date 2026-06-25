using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Accord.Services.Starboard;
using MediatR;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Rest.Core;

namespace Accord.Bot.RequestHandlers;

public class RelayStarboardEntryToDiscordHandler(
    EmbedFactory embedFactory,
    IDiscordRestChannelAPI channelApi,
    JumpLinkHelper jumpLinkHelper)
    : IRequestHandler<RelayNewStarboardEntryToDiscordRequest, ulong?>,
        IRequestHandler<RelayExistingStarboardEntryToDiscordRequest>,
        IRequestHandler<DeleteStarboardEntryToDiscordRequest>
{
    public async Task<ulong?> Handle(RelayNewStarboardEntryToDiscordRequest request, CancellationToken cancellationToken)
    {
        var messageSnowflake = new Snowflake(request.StarredDiscordMessageId);
        var channelSnowflake = new Snowflake(request.StarredDiscordMessageChannelId);

        var channel = await channelApi.GetChannelAsync(channelSnowflake, cancellationToken);

        if (!channel.IsSuccess)
            return null;
        
        var starredMessage = await channelApi.GetChannelMessageAsync(channelSnowflake, messageSnowflake, cancellationToken);

        if (!starredMessage.IsSuccess)
            return null;

        var embedBuilder = embedFactory.FromMessage(starredMessage.Entity);
        
        var jumpLink = jumpLinkHelper.FromMessage(starredMessage.Entity);
        var markdownLink = $"#{channel.Entity.Name} [(click here)]({jumpLink})";
        embedBuilder.AddField(new EmbedField("Posted in", $"**{markdownLink}**"));
        
        var embed = embedBuilder.Build();

        if (!embed.IsSuccess)
            return null;
        
        var reply = await channelApi.CreateMessageAsync(new Snowflake(request.PostToDiscordChannelId),
            $"{request.StarEmoji} {request.NumberOfStars}",
            embeds: new [] { embed.Entity },
            allowedMentions: new AllowedMentions(Parse: new List<MentionType>()),
            ct: cancellationToken);

        return reply.IsSuccess ? reply.Entity.ID.Value : null;
    }

    public async Task Handle(RelayExistingStarboardEntryToDiscordRequest request, CancellationToken cancellationToken)
    {
        var messageSnowflake = new Snowflake(request.StarredDiscordMessageId);
        var channelSnowflake = new Snowflake(request.StarredDiscordMessageChannelId);

        var channel = await channelApi.GetChannelAsync(channelSnowflake, cancellationToken);

        if (!channel.IsSuccess)
            return;
        
        var starredMessage = await channelApi.GetChannelMessageAsync(channelSnowflake, messageSnowflake, cancellationToken);

        if (!starredMessage.IsSuccess)
            return;

        var embedBuilder = embedFactory.FromMessage(starredMessage.Entity);
        
        var jumpLink = jumpLinkHelper.FromMessage(starredMessage.Entity);
        var markdownLink = $"#{channel.Entity.Name} [(click here)]({jumpLink})";
        embedBuilder.AddField(new EmbedField("Posted in", $"**{markdownLink}**"));
        
        var embed = embedBuilder.Build();

        if (!embed.IsSuccess)
            return;
        
        await channelApi.EditMessageAsync(new Snowflake(request.DiscordMessageChannelIdToEdit),
            new Snowflake(request.DiscordMessageIdToEdit),
            $"{request.StarEmoji} {request.NumberOfStars}",
            embeds: new [] { embed.Entity },
            allowedMentions: new AllowedMentions(Parse: new List<MentionType>()),
            ct: cancellationToken);
    }

    public async Task Handle(DeleteStarboardEntryToDiscordRequest request, CancellationToken cancellationToken)
    {
        var messageSnowflake = new Snowflake(request.DiscordMessageIdToEdit);
        var channelSnowflake = new Snowflake(request.DiscordMessageChannelIdToEdit);
        await channelApi.DeleteMessageAsync(channelSnowflake, messageSnowflake, ct: cancellationToken);
    }
}