using System;
using System.Text.Json.Serialization;

namespace Accord.Bot.Responders.Eval;

public class ReplResult
{
    [JsonPropertyName("returnTypeName")]
    public string? ReturnTypeName { get; set; }
    
    [JsonPropertyName("returnValue")]
    public object? ReturnValue { get; set; }
    
    [JsonPropertyName("exception")]
    public string? Exception { get; set; }
    
    [JsonPropertyName("exceptionType")]
    public string? ExceptionType { get; set; }
    
    [JsonPropertyName("consoleOut")]
    public string? ConsoleOut { get; set; }
    
    [JsonPropertyName("code")]
    public required string Code { get; set; }
    
    [JsonPropertyName("executionTime")]
    public TimeSpan ExecutionTime { get; set; }
    
    [JsonPropertyName("compileTime")]
    public TimeSpan CompileTime { get; set; }
}