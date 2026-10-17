using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services.Tags;

public sealed record GetTagsRequest(int Page = 1, int PageSize = 25, string? Search = null) : IRequest<GetTagsResponse>;

public sealed record GetTagsResponse(IReadOnlyList<TagOverviewDto> Tags, int Page, int PageSize, int TotalCount, string? Search)
{
    public int TotalPages => Math.Max(1, (int)Math.Ceiling((double)TotalCount / PageSize));
    public int FirstItem => TotalCount == 0 ? 0 : (Page - 1) * PageSize + 1;
    public int LastItem => TotalCount == 0 ? 0 : FirstItem + Tags.Count - 1;
}

public sealed record TagOverviewDto(
    int Id,
    string Name,
    IReadOnlyCollection<string> Aliases,
    string Content,
    int Uses,
    ulong AddedByDiscordUserId,
    string? AddedByDiscordUsername,
    DateTimeOffset AddedDateTime
);

public sealed class GetTagsHandler(AccordContext db) : IRequestHandler<GetTagsRequest, GetTagsResponse>
{
    public async Task<GetTagsResponse> Handle(GetTagsRequest request, CancellationToken cancellationToken)
    {
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var search = string.IsNullOrWhiteSpace(request.Search) ? null : request.Search.Trim();
        var query = db.Tags.AsNoTracking();
        if (search is not null)
        {
            var pattern = $"%{search.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_")}%";
            query = query.Where(tag => EF.Functions.ILike(tag.Content, pattern, "\\") ||
                tag.Aliases.Any(alias => EF.Functions.ILike(alias.Name, pattern, "\\")));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = Math.Max(1, (int)Math.Ceiling((double)totalCount / pageSize));
        var page = Math.Clamp(request.Page, 1, totalPages);
        var tags = await query
            .OrderByDescending(t => t.Uses)
            .ThenBy(t => t.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new TagOverviewDto(
                t.Id,
                t.Aliases.OrderBy(a => a.AddedDateTime).ThenBy(a => a.Id).Select(a => a.Name).FirstOrDefault() ?? "Unnamed tag",
                t.Aliases.OrderBy(a => a.AddedDateTime).ThenBy(a => a.Id).Select(a => a.Name).ToList(),
                t.Content,
                t.Uses,
                t.AddedByUserId,
                t.AddedByUser!.Username,
                t.AddedDateTime
            ))
            .ToListAsync(cancellationToken);

        return new GetTagsResponse(tags, page, pageSize, totalCount, search);
    }
}
