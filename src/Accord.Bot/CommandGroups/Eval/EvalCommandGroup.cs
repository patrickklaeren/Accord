using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Accord.Services.UserBotMessages;
using MediatR;
using Remora.Commands.Attributes;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Results;

namespace Accord.Bot.CommandGroups.Eval;

public class EvalCommandGroup(ICommandContext commandContext,
    IDiscordRestChannelAPI channelApi,
    JumpLinkHelper jumpLinkHelper,
    ThumbnailHelper thumbnailHelper,
    FeedbackService feedbackService,
    HttpClient httpClient,
    IMediator mediator) : AccordCommandGroup
{
    [Command("eval", "repl", "exec", "e")]
    public async Task<IResult> Eval([Greedy] string content)
    {
        var executingUser = commandContext.GetExecutingUser();
        var avatar = thumbnailHelper.GetAvatar(executingUser);

        var message = commandContext.TryGetMessage();
        var replDescription = "Compiling and executing your code now...";

        if (message is not null)
        {
            var jumpLink = jumpLinkHelper.FromMessage(message);
            replDescription = $"Compiling and executing [your code]({jumpLink}) now...";
        }
        
        var embed = new Embed(Title: "C# REPL executing...",
            Colour: Color.Orange,
            Author: new EmbedAuthor(executingUser.Username, IconUrl: avatar.Url),
            Description: replDescription);

        var workingMessageResult = await feedbackService.SendContextualEmbedAsync(embed);
        var workingMessage = workingMessageResult.Entity;

        await mediator.Publish(new AddUserBotMessageRequest(workingMessage.ID.Value,
            workingMessage.ChannelID.Value,
            executingUser.ID.Value));
        
        var sanitised = EvalHelper.Sanitise(content);

        try
        {
            var response = await httpClient.PostAsync("Eval", new StringContent(sanitised, Encoding.UTF8, "text/plain"));

            if (!response.IsSuccessStatusCode)
            {
                await RespondWithErrorEmbed($"The request failed with a status code of {(int)response.StatusCode} ({response.ReasonPhrase})");
                return Result.FromSuccess();
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var replResult = JsonSerializer.Deserialize<ReplResult>(responseContent);

            if (replResult is null)
            {
                await RespondWithErrorEmbed("The response did not deserialise into a known type");
                return Result.FromSuccess();
            }
            
            var successEmbed = GetSuccessEmbed(executingUser, replResult);
            await channelApi.EditMessageAsync(workingMessage.ChannelID, workingMessage.ID, embeds: new[] { successEmbed });
        }
        catch (Exception ex)
        {
            await RespondWithErrorEmbed(ex.Message);
        }

        if (message is not null)
        {
            await channelApi.DeleteMessageAsync(message.ChannelID, message.ID);
        }

        return Result.FromSuccess();

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

    private Embed GetSuccessEmbed(IUser executingUser, ReplResult parsedResult)
    {
        var avatar = thumbnailHelper.GetAvatar(executingUser);
        
        var consoleOut = parsedResult.ConsoleOut;
        var hasException = !string.IsNullOrEmpty(parsedResult.Exception);
        var status = hasException ? "failed" : "succeeded";

        var fields = new List<EmbedField>();
        
        if (parsedResult.ReturnValue is not null)
        {
            var title = EvalHelper.TruncateToEmbedField($"Result: {parsedResult.ReturnTypeName}");
            var value = EvalHelper.FormatAsCodeBlock(EvalHelper.TruncateToEmbedField(parsedResult.ReturnValue.ToString()!), "json");
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
            var exceptionMadeNice = EvalHelper.MakeRawExceptionNiceForDiscordEmbed(parsedResult.Exception!);
            var title = EvalHelper.TruncateToEmbedField($"Exception: {parsedResult.ExceptionType}");
            var value = EvalHelper.FormatAsCodeBlock(EvalHelper.TruncateToEmbedField(exceptionMadeNice), "diff");
            fields.Add(new EmbedField(title, value, false));
        }

        var embed = new Embed(Title: $"C# REPL {status}",
            Colour: hasException ? Color.Red : Color.Green,
            Author: new EmbedAuthor(executingUser.Username, IconUrl: avatar.Url),
            Description: EvalHelper.FormatAsCodeBlock(parsedResult.Code),
            Fields: fields,
            Footer: new EmbedFooter($"{parsedResult.CompileTime.TotalMilliseconds:F}ms to compile | {parsedResult.ExecutionTime.TotalMilliseconds:F}ms to execute"));
        
        return embed;
    }
}