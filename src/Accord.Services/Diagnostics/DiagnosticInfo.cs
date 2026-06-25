namespace Accord.Services.Diagnostics;

public sealed record DiagnosticInfo(
    string Code,
    string Message,
    string Severity,
    string Category,
    string? Url,
    string? Description);