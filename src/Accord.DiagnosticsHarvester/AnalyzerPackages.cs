using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Accord.DiagnosticsHarvester;

public static class AnalyzerPackages
{
    public static IEnumerable<string> ResolveDllPaths()
    {
        var root = Path.Combine(AppContext.BaseDirectory, "analyzers");

        if (!Directory.Exists(root))
        {
            Console.Error.WriteLine($"Analyzer DLL directory not found: {root}");
            return [];
        }

        return Directory.EnumerateFiles(root, "*.dll", SearchOption.AllDirectories)
            .Where(path => !path.Replace('\\', '/').Contains("/vb/", StringComparison.OrdinalIgnoreCase)) // skip vb
            .Where(path =>
            {
                // skip non-analyzer assemblies
                var file = Path.GetFileName(path);
                return file.Contains("Analyzers", StringComparison.OrdinalIgnoreCase)
                    || file.Contains("CodeStyle", StringComparison.OrdinalIgnoreCase);
            })
            .ToList();
    }
}