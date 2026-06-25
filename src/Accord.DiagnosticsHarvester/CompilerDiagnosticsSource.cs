using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp;

namespace Accord.DiagnosticsHarvester;

public static class CompilerDiagnosticsSource
{
    private const string HELP_URL_BASE =
        "https://learn.microsoft.com/dotnet/csharp/language-reference/compiler-messages/";

    public static IReadOnlyList<DiagnosticRecord> Collect()
    {
        var diagnostics = new List<DiagnosticRecord>();

        var roslyn = typeof(CSharpCompilation).Assembly;

        // internal enum ErrorCode
        var errorCodeType = roslyn.GetType("Microsoft.CodeAnalysis.CSharp.ErrorCode", throwOnError: true)!;

        // internal static class ErrorFacts
        var errorFactsType = roslyn.GetType("Microsoft.CodeAnalysis.CSharp.ErrorFacts", throwOnError: true)!;

        // public static string ErrorFacts.GetMessage(ErrorCode code, CultureInfo culture)
        var getMessage = errorFactsType.GetMethod(
            "GetMessage",
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types: [errorCodeType, typeof(CultureInfo)],
            modifiers: null)
            ?? throw new InvalidOperationException(
                "ErrorFacts.GetMessage(ErrorCode, CultureInfo) was not found. The Roslyn API surface may have changed.");

        foreach (var name in Enum.GetNames(errorCodeType))
        {
            if (!HasDiagnosticPrefix(name))
            {
                continue;
            }

            var value = (int)Enum.Parse(errorCodeType, name);
            if (value <= 0)
            {
                continue;
            }

            var errorCode = Enum.ToObject(errorCodeType, value);
            var message = getMessage.Invoke(null, [errorCode, CultureInfo.InvariantCulture]) as string;

            if (string.IsNullOrWhiteSpace(message))
            {
                continue;
            }

            var code = $"CS{value:D4}";
            diagnostics.Add(new DiagnosticRecord(code, message, HELP_URL_BASE + code.ToLowerInvariant()));
        }

        return diagnostics;
    }

    private static bool HasDiagnosticPrefix(string name) =>
        name.StartsWith("ERR_", StringComparison.Ordinal)
        || name.StartsWith("WRN_", StringComparison.Ordinal)
        || name.StartsWith("INF_", StringComparison.Ordinal)
        || name.StartsWith("HDN_", StringComparison.Ordinal)
        || name.StartsWith("FTL_", StringComparison.Ordinal);
}