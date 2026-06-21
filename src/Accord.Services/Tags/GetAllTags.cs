using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services.Tags;

public sealed record GetAllTagsRequest : IRequest<List<TagOverviewDto>>;

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

public class GetAllTagsHandler(AccordContext db) : IRequestHandler<GetAllTagsRequest, List<TagOverviewDto>>
{
    public async Task<List<TagOverviewDto>> Handle(GetAllTagsRequest request, CancellationToken cancellationToken)
    {
        return await db.Tags
            .OrderByDescending(t => t.Uses)
            .Select(t => new TagOverviewDto(
                t.Id,
                t.Aliases.OrderBy(a => a.AddedDateTime).Select(a => a.Name).FirstOrDefault()!,
                t.Aliases.OrderBy(a => a.AddedDateTime).Select(a => a.Name).ToList(),
                t.Content,
                t.Uses,
                t.AddedByUserId,
                t.AddedByUser!.Username,
                t.AddedDateTime
            ))
            .ToListAsync(cancellationToken);
    }
}
