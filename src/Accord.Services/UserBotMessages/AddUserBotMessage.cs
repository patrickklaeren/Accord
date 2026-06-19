using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using Accord.Domain.Model;
using MediatR;

namespace Accord.Services.UserBotMessages;

public sealed record AddUserBotMessageRequest(ulong DiscordMessageId, ulong DiscordChannelId, ulong DiscordUserId) : INotification;

public class AddUserBotMessageHandler(AccordContext db) : INotificationHandler<AddUserBotMessageRequest>
{
    public async Task Handle(AddUserBotMessageRequest request, CancellationToken cancellationToken)
    {
        var userBotMessage = new UserBotMessage
        {
            Id = request.DiscordMessageId,
            DiscordChannelId =  request.DiscordChannelId,
            UserId = request.DiscordUserId,
        };
        
        db.UserBotMessages.Add(userBotMessage);
        await db.SaveChangesAsync(cancellationToken);
    }
}