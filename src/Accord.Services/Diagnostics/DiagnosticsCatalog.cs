using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Accord.Services.Diagnostics;

[RegisterSingleton]
public class DiagnosticsCatalog
{
    private const string RESOURCE_SUFFIX = "diagnostics.json";

    private readonly Lazy<Catalog> _catalog = new(Load);

    public IReadOnlyList<DiagnosticInfo> Search(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        var catalog = _catalog.Value;
        var normalized = Normalize(query);

        if (catalog.ByCode.TryGetValue(normalized, out var exact))
        {
            return [exact];
        }

        // Codes is pre-sorted, so the prefix matches come back ordered.
        return catalog.Codes
            .Where(code => code.StartsWith(normalized, StringComparison.OrdinalIgnoreCase))
            .Select(code => catalog.ByCode[code])
            .ToList();
    }

    public IReadOnlyList<DiagnosticInfo> Suggest(string query, int limit)
    {
        var trimmed = query.Trim();

        if (trimmed.Length == 0)
        {
            return [];
        }

        var catalog = _catalog.Value;

        var prefixed = catalog.Codes
            .Where(code => code.StartsWith(trimmed, StringComparison.OrdinalIgnoreCase));

        var contained = catalog.Codes
            .Where(code => !code.StartsWith(trimmed, StringComparison.OrdinalIgnoreCase)
                && code.Contains(trimmed, StringComparison.OrdinalIgnoreCase));

        return prefixed.Concat(contained)
            .Take(limit)
            .Select(code => catalog.ByCode[code])
            .ToList();
    }

    private static string Normalize(string query)
    {
        var trimmed = query.Trim().ToUpperInvariant();

        var letters = 0;
        while (letters < trimmed.Length && char.IsLetter(trimmed[letters]))
        {
            letters++;
        }

        if (letters > 0 && letters < trimmed.Length)
        {
            var prefix = trimmed[..letters];
            var digits = trimmed[letters..];

            if (digits.All(char.IsDigit))
            {
                return prefix + digits.PadLeft(4, '0');
            }
        }

        return trimmed;
    }

    private static Catalog Load()
    {
        var assembly = typeof(DiagnosticsCatalog).Assembly;

        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(name => name.EndsWith(RESOURCE_SUFFIX, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Embedded diagnostics resource ending in '{RESOURCE_SUFFIX}' was not found.");

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Could not open embedded resource '{resourceName}'.");

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        var entries = JsonSerializer.Deserialize<List<DiagnosticInfo>>(stream, options)
            ?? throw new InvalidOperationException("Diagnostics resource deserialized to null.");

        var byCode = new Dictionary<string, DiagnosticInfo>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in entries)
        {
            byCode[entry.Code] = entry;
        }

        var codes = byCode.Keys
            .OrderBy(code => code, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new Catalog(byCode, codes);
    }

    private sealed record Catalog(IReadOnlyDictionary<string, DiagnosticInfo> ByCode, IReadOnlyList<string> Codes);
}