namespace Accord.DiagnosticsHarvester;

public sealed record DiagnosticRecord(
    string Code,
    string Message,
    string Severity,
    string Category,
    string? Url,
    string? Description);