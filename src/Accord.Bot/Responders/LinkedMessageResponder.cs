using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Accord.Services;
using Accord.Services.UserBotMessages;
using MediatR;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Gateway.Responders;
using Remora.Rest.Core;
using Remora.Results;

namespace Accord.Bot.Responders;

public partial class LinkedMessageResponder(
    DiscordConfiguration discordConfiguration,
    JumpLinkHelper jumpLinkHelper,
    EmbedFactory embedFactory,
    IDiscordRestChannelAPI channelApi,
    IMediator mediator) : IResponder<IMessageCreate>
{
    [GeneratedRegex(
        @"^(?<Prelink>[\s\S]*?)?(?<OpenBrace><)?https?://(?:(?:ptb|canary)\.)?discord(app)?\.com/channels/(?<GuildId>\d+)/(?<ChannelId>\d+)/(?<MessageId>\d+)/?(?<CloseBrace>>)?(?<Postlink>[\s\S]*)?$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private partial Regex DiscordMessageLinkRegex();

    public async Task<Result> RespondAsync(IMessageCreate gatewayEvent, CancellationToken ct = new CancellationToken())
    {
        if (gatewayEvent.Author.IsBot.HasValue || gatewayEvent.Author.IsSystem.HasValue)
            return Result.FromSuccess();

        var matches = DiscordMessageLinkRegex().Matches(gatewayEvent.Content);

        foreach (Match match in matches)
        {
            // check if the link is surrounded with < and >. This was too annoying to do in regex
            if (match.Groups["OpenBrace"].Success && match.Groups["CloseBrace"].Success)
                continue;

            if (!ulong.TryParse(match.Groups["GuildId"].Value, out var guildId)
                || !ulong.TryParse(match.Groups["ChannelId"].Value, out var channelId)
                || !ulong.TryParse(match.Groups["MessageId"].Value, out var messageId))
            {
                continue;
            }
            
            if (guildId != discordConfiguration.GuildId)
                continue;

            try
            {

                var channelSnowflake = new Snowflake(channelId);

                var channel = await channelApi.GetChannelAsync(channelSnowflake, ct);

                if (!channel.IsSuccess)
                    continue;

                if (channel.Entity.IsNsfw is { HasValue: true, Value: true })
                    continue;

                var messageSnowflake = new Snowflake(messageId);

                var message = await channelApi.GetChannelMessageAsync(channelSnowflake, messageSnowflake, ct);

                if (!message.IsSuccess)
                    continue;

                // If the message we downloaded has an embed with "Quoted by" in it, it's
                // likely the user has linked a quote in itself, so skip this message
                if (message.Entity.Embeds.Any(d => d.Fields.HasValue && d.Fields.Value.Any(c => c.Name == "Quoted by")))
                    continue;

                var embedBuilder = embedFactory.FromMessage(message.Entity);
                
                var jumpLink = jumpLinkHelper.FromMessage(message.Entity);
                var markdownLink = $"#{channel.Entity.Name} [(click here)]({jumpLink})";
                embedBuilder.AddField(new EmbedField("Quoted by", $"{gatewayEvent.Author.ID.ToUserMention()} from **{markdownLink}**"));
                
                var resultEmbed = embedBuilder.Build();

                if (!resultEmbed.IsSuccess)
                    continue;
                
                var embedMessage = await channelApi.CreateMessageAsync(
                    gatewayEvent.ChannelID,
                    embeds: new[] { resultEmbed.Entity },
                    allowedMentions: new AllowedMentions(MentionRepliedUser: false),
                    messageReference: gatewayEvent.MessageReference,
                    ct: ct);

                if (!embedMessage.IsSuccess)
                    continue;

                await mediator.Publish(new AddUserBotMessageRequest(embedMessage.Entity.ID.Value,
                        embedMessage.Entity.ChannelID.Value,
                        gatewayEvent.Author.ID.Value),
                    ct);

                if (string.IsNullOrEmpty(match.Groups["Prelink"].Value)
                    && string.IsNullOrEmpty(match.Groups["Postlink"].Value))
                {
                    await channelApi.DeleteMessageAsync(gatewayEvent.ChannelID, gatewayEvent.ID, ct: ct);
                }
            }
            catch
            {
                // Do nothing
            }
        }

        return Result.FromSuccess();
    }
}