using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Accord.Services.Tags;

namespace Accord.Web.Models;

public sealed record TagsViewModel(GetTagsResponse Results, bool CanManage);

public sealed record TagInputModel
{
    [Required, StringLength(100)]
    public string Name { get; init; } = string.Empty;

    [Required]
    public string Content { get; init; } = string.Empty;
}

public sealed record EditTagViewModel(int Id, string Name, TagContentInputModel Input, IReadOnlyList<TagAliasDto> Aliases);

public sealed record TagAliasInputModel
{
    [Required, StringLength(100)]
    public string Name { get; init; } = string.Empty;
}

public sealed record TagContentInputModel
{
    [Required]
    public string Content { get; init; } = string.Empty;
}
