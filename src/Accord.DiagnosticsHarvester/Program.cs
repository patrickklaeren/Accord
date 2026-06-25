using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Accord.DiagnosticsHarvester;

var outputPath = Path.GetFullPath(args.Length > 0 ? args[0] : DefaultOutputPath());

var records = new Dictionary<string, DiagnosticRecord>(StringComparer.OrdinalIgnoreCase);

Console.WriteLine("Collecting compiler diagnostics...");
AddAll(records, CompilerDiagnosticsSource.Collect());
Console.WriteLine($"  total: {records.Count}");

Console.WriteLine("Collecting analyzer diagnostics...");
var analyzerDlls = AnalyzerPackages.ResolveDllPaths().ToList();
foreach (var dll in analyzerDlls)
{
    Console.WriteLine($"  source: {Path.GetFileName(dll)}");
}
AddAll(records, AnalyzerDiagnosticsSource.Collect(analyzerDlls));
Console.WriteLine($"  total after analyzers: {records.Count}");

var ordered = records.Values
    .OrderBy(record => record.Code, StringComparer.OrdinalIgnoreCase)
    .ToList();

var json = JsonSerializer.Serialize(ordered, new JsonSerializerOptions
{
    WriteIndented = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
});

Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
File.WriteAllText(outputPath, json);

Console.WriteLine($"Wrote {ordered.Count} diagnostics to {outputPath}");
return 0;

static void AddAll(IDictionary<string, DiagnosticRecord> target, IReadOnlyList<DiagnosticRecord> records)
{
    foreach (var record in records)
    {
        target[record.Code] = record;
    }
}

static string DefaultOutputPath() =>
    Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..",
        "Accord.Services", "Diagnostics", "diagnostics.json");