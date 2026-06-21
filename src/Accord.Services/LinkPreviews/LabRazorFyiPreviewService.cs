using System;
using System.Buffers.Text;
using System.Collections.Immutable;
using System.IO;
using System.IO.Compression;
using System.Text.RegularExpressions;
using ProtoBuf;

namespace Accord.Services.LinkPreviews;

public static partial class LabRazorFyiPreviewService
{
    public static string? TryGetPreview(string fragment)
    {
        if (string.IsNullOrWhiteSpace(fragment))
        {
            return null;
        }

        try
        {
            var bytes = Base64Url.DecodeFromChars(fragment.AsSpan(1));
            using var deflateStream = new DeflateStream(new MemoryStream(bytes), CompressionMode.Decompress);

            var savedState = Serializer.Deserialize<LabRazorFyiSavedState>(deflateStream);
            var selectedFile = savedState.Inputs[savedState.SelectedInputIndex];

            if (selectedFile.FileExtension != ".cs")
            {
                return null;
            }

            return RemoveUsings(selectedFile.Text);
        }
        catch
        {
            return null;
        }
    }
    
    [GeneratedRegex(@"using \w+(?:\.\w+)*;",  RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex UsingsRegex();
    private static string RemoveUsings(string sourceCode) => UsingsRegex().Replace(sourceCode, string.Empty);
}

// the below definitions were taken from https://github.com/jjonescz/DotNetLab at commit dedcefec241a1d32fe8a6683ccaa39ff40dc1730
// as such they are licensed under the MIT license in that repo, https://github.com/jjonescz/DotNetLab/blob/dedcefec241a1d32fe8a6683ccaa39ff40dc1730/LICENSE
[ProtoContract]
sealed file record LabRazorFyiInputCode
{
    [ProtoMember(1)]
    public required string FileName { get; init; }
    [ProtoMember(2)]
    public required string Text { get; init; }

    public string FileExtension => Path.GetExtension(FileName);
}

[ProtoContract]
sealed file record LabRazorFyiSavedState
{
    [ProtoMember(1)]
    public ImmutableArray<LabRazorFyiInputCode> Inputs { get; init; }

    [ProtoMember(8)]
    public int SelectedInputIndex { get; init; }
}