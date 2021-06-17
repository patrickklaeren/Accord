using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Bot.Infrastructure;
using Accord.Domain.Model;
using Accord.Services.ChannelFlags;
using MediatR;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Core;

namespace Accord.Bot.RequestHandlers
{
    public class LogHandler : INotificationHandler<LogNotification>
    {
        private readonly IDiscordRestChannelAPI _channelApi;
        private readonly IMediator _mediator;

        public LogHandler(IDiscordRestChannelAPI channelApi, IMediator mediator)
        {
            _channelApi = channelApi;
            _mediator = mediator;
        }

        public async Task Handle(LogNotification notification, CancellationToken cancellationToken)
        {
            var channelsToPostTo = await _mediator.Send(new GetChannelsWithFlagRequest(ChannelFlagType.BotLogs), cancellationToken);

            if (!channelsToPostTo.Any())
                return;

            var fields = new List<EmbedField>();

            if (!string.IsNullOrWhiteSpace(notification.LogEvent.MessageTemplate.Text))
            {
                fields.Add(new EmbedField("Message", $"```{notification.LogEvent.RenderMessage()}```"));
            }

            if (notification.LogEvent.Exception is not null)
            {
                fields.Add(new EmbedField("Exception", $"```{notification.LogEvent.Exception.Message}```"));
            }

            var embed = new Embed(Title: notification.LogEvent.Level.ToString(), Fields: fields);

            foreach (var channel in channelsToPostTo)
            {
                await _channelApi.CreateMessageAsync(new Snowflake(channel), embed: embed, ct: cancellationToken);
            }
        }
    }
}