using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Accord.Services.CodeEvaluation;
using Accord.Services.UserBotMessages;
using MediatR;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Gateway.Responders;
using Remora.Results;

namespace Accord.Bot.Responders.Eval;

public class EvalResponder(ThumbnailHelper thumbnailHelper,
    JumpLinkHelper jumpLinkHelper,
    IDiscordRestChannelAPI channelApi,
    IMediator mediator) : IResponder<IMessageCreate>
{
    private const string COMMAND_ONE = "!eval";
    private const string COMMAND_TWO = "!exec";
    private const string COMMAND_THREE = "!e";
    
    public async Task<Result> RespondAsync(IMessageCreate gatewayEvent, CancellationToken ct = new CancellationToken())
    {
        if (gatewayEvent.Author.IsBot.HasValue || gatewayEvent.Author.IsSystem.HasValue)
            return Result.FromSuccess();

        var trimmed = gatewayEvent.Content.Trim();

        if (trimmed.StartsWith(COMMAND_ONE, StringComparison.OrdinalIgnoreCase) 
            || trimmed.StartsWith(COMMAND_TWO, StringComparison.OrdinalIgnoreCase) 
            || trimmed.StartsWith(COMMAND_THREE, StringComparison.OrdinalIgnoreCase))
        {
            await ExecuteEval(gatewayEvent.Author, gatewayEvent);
        }

        return Result.FromSuccess();
    }

    private async Task ExecuteEval(IUser executingUser, IMessageCreate message)
    {
        var avatar = thumbnailHelper.GetAvatar(executingUser);

        var jumpLink = jumpLinkHelper.FromMessage(message);
        var replDescription = $"Compiling and executing [your code]({jumpLink}) now...";
        
        var embed = new Embed(Title: "C# REPL executing...",
            Colour: Color.Orange,
            Author: new EmbedAuthor(executingUser.Username, IconUrl: avatar.Url),
            Description: replDescription);

        var workingMessageResult = await channelApi.CreateMessageAsync(
            message.ChannelID,
            embeds: new[] { embed },
            allowedMentions: new AllowedMentions(MentionRepliedUser: false)
        );

        var workingMessage = workingMessageResult.Entity;

        await mediator.Publish(new AddUserBotMessageRequest(workingMessage.ID.Value,
            workingMessage.ChannelID.Value,
            executingUser.ID.Value));
        
        // We need to sanitise the input from the command
        var trimmed = message.Content.Trim();
        string[] prefixes = [COMMAND_ONE, COMMAND_TWO, COMMAND_THREE];
        var prefix = prefixes.First(x => trimmed.StartsWith(x, StringComparison.OrdinalIgnoreCase));
        
        var expression = trimmed[prefix.Length..].Trim();
        var sanitised = EvalHelper.Sanitise(expression);

        try
        {
            var replResult = await mediator.Send(new ExecuteEvalRequest(sanitised));

            if (!replResult.Success)
            {
                await RespondWithErrorEmbed(replResult.ErrorMessage);
                return;
            }
            
            var successEmbed = GetSuccessEmbed(executingUser, replResult.Value!);
            await channelApi.EditMessageAsync(workingMessage.ChannelID, workingMessage.ID, embeds: new[] { successEmbed });
        }
        catch (Exception ex)
        {
            await RespondWithErrorEmbed(ex.Message);
        }

        await channelApi.DeleteMessageAsync(message.ChannelID, message.ID);

        return;

        async Task RespondWithErrorEmbed(string description)
        {
            var errorEmbed = GetErrorEmbed(executingUser, description);
            await channelApi.EditMessageAsync(workingMessage.ChannelID, workingMessage.ID, embeds: new[] { errorEmbed });
        }
    }

    private Embed GetErrorEmbed(IUser executingUser, string description)
    {
        var avatar = thumbnailHelper.GetAvatar(executingUser);
        var field = new EmbedField("What went wrong?", description, false);

        var errorEmbed = new Embed(Title: "C# REPL failed executing!",
            Colour: Color.Red,
            Author: new EmbedAuthor(executingUser.Username, IconUrl: avatar.Url),
            Description: "Something went wrong. You can try again by re-running the command.",
            Fields: new [] { field },
            Footer: new EmbedFooter("React to this message with ❌ or 🗑️ to remove it️"));
        
        return errorEmbed;
    }

    private Embed GetSuccessEmbed(IUser executingUser, ExecuteEvalResultDto evalResult)
    {
        var avatar = thumbnailHelper.GetAvatar(executingUser);
        
        var consoleOut = evalResult.ConsoleOut;
        var hasException = !string.IsNullOrEmpty(evalResult.Exception);
        var status = hasException ? "failed" : "succeeded";

        var fields = new List<EmbedField>();
        
        if (evalResult.ReturnValue is not null)
        {
            var title = EvalHelper.TruncateToEmbedField($"Result: {evalResult.ReturnTypeName}");
            var value = EvalHelper.FormatAsCodeBlock(EvalHelper.TruncateToEmbedField(evalResult.ReturnValue.ToString()!), "json");
            fields.Add(new EmbedField(title, value, false));
        }

        if (!string.IsNullOrWhiteSpace(consoleOut))
        {
            var title = EvalHelper.TruncateToEmbedField("Console Output");
            var value = EvalHelper.FormatAsCodeBlock(EvalHelper.TruncateToEmbedField(consoleOut), "txt");
            fields.Add(new EmbedField(title, value, false));
        }

        if (hasException)
        {
            var exceptionMadeNice = EvalHelper.MakeRawExceptionNiceForDiscordEmbed(evalResult.Exception!);
            var title = EvalHelper.TruncateToEmbedField($"Exception: {evalResult.ExceptionType}");
            var value = EvalHelper.FormatAsCodeBlock(EvalHelper.TruncateToEmbedField(exceptionMadeNice), "diff");
            fields.Add(new EmbedField(title, value, false));
        }

        if (!string.IsNullOrWhiteSpace(evalResult.ResultPasteUrl))
        {
            fields.Add(new EmbedField("Full output", evalResult.ResultPasteUrl));   
        }
        
        var embed = new Embed(Title: $"C# REPL {status}",
            Colour: hasException ? Color.Red : Color.Green,
            Author: new EmbedAuthor(executingUser.Username, IconUrl: avatar.Url),
            Description: EvalHelper.FormatAsCodeBlock(evalResult.Code),
            Fields: fields,
            Footer: new EmbedFooter($"{evalResult.CompileTime.TotalMilliseconds:F}ms to compile | {evalResult.ExecutionTime.TotalMilliseconds:F}ms to execute"));
        
        return embed;
    }
}