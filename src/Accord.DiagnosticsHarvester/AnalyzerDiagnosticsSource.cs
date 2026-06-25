using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Accord.DiagnosticsHarvester;

public static partial class AnalyzerDiagnosticsSource
{
    public static IReadOnlyList<DiagnosticRecord> Collect(IEnumerable<string> analyzerDllPaths)
    {
        var byCode = new Dictionary<string, DiagnosticRecord>(StringComparer.OrdinalIgnoreCase);

        foreach (var dllPath in analyzerDllPaths)
        {
            Assembly assembly;
            try
            {
                assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(dllPath);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to load {Path.GetFileName(dllPath)}: {ex.Message}");
                continue;
            }

            foreach (var descriptor in ReadDescriptors(assembly))
            {
                if (!IsDiagnosticCode(descriptor.Id))
                {
                    continue;
                }

                var message = Materialize(descriptor.Title, descriptor.MessageFormat);
                if (string.IsNullOrWhiteSpace(message))
                {
                    continue;
                }

                byCode[descriptor.Id] = new DiagnosticRecord(
                    descriptor.Id,
                    message,
                    descriptor.DefaultSeverity.ToString(),
                    descriptor.Category,
                    string.IsNullOrWhiteSpace(descriptor.HelpLinkUri) ? null : descriptor.HelpLinkUri,
                    Describe(descriptor.Description));
            }
        }

        return byCode.Values.ToList();
    }

    private static IEnumerable<DiagnosticDescriptor> ReadDescriptors(Assembly assembly)
    {
        Type[] types;
        try
        {
            types = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            Console.Error.WriteLine($"Partial load for {assembly.GetName().Name}: {ex.LoaderExceptions.Length} loader exception(s)");
            types = ex.Types.Where(type => type is not null).ToArray()!;
        }

        foreach (var type in types)
        {
            if (type is null || type.IsAbstract)
            {
                continue;
            }

            if (!typeof(DiagnosticAnalyzer).IsAssignableFrom(type))
            {
                continue;
            }

            if (type.GetCustomAttributes(typeof(DiagnosticAnalyzerAttribute), inherit: false).Length == 0)
            {
                continue;
            }

            DiagnosticAnalyzer analyzer;
            try
            {
                analyzer = (DiagnosticAnalyzer)Activator.CreateInstance(type)!;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Could not instantiate {type.FullName}: {ex.Message}");
                continue;
            }

            ImmutableArray<DiagnosticDescriptor> descriptors;
            try
            {
                descriptors = analyzer.SupportedDiagnostics;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"SupportedDiagnostics threw for {type.FullName}: {ex.Message}");
                continue;
            }

            foreach (var descriptor in descriptors)
            {
                yield return descriptor;
            }
        }
    }

    private static string Materialize(LocalizableString title, LocalizableString messageFormat)
    {
        var text = title.ToString(CultureInfo.InvariantCulture);

        if (string.IsNullOrWhiteSpace(text))
        {
            text = messageFormat.ToString(CultureInfo.InvariantCulture);
        }

        return text;
    }

    private static string? Describe(LocalizableString description)
    {
        var text = description.ToString(CultureInfo.InvariantCulture);
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    [GeneratedRegex(@"^[A-Za-z]+[0-9]+$")]
    private static partial Regex DiagnosticCodeRegex { get; }

    private static bool IsDiagnosticCode(string id) =>
        DiagnosticCodeRegex.IsMatch(id);
}