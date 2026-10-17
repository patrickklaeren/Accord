using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services.Tags;

public sealed record GetTagByIdRequest(int Id) : IRequest<TagDetailsDto?>;
public sealed record TagAliasDto(int Id, string Name);
public sealed record TagDetailsDto(int Id, string Name, string Content, IReadOnlyList<TagAliasDto> Aliases);

public sealed class GetTagByIdHandler(AccordContext db) : IRequestHandler<GetTagByIdRequest, TagDetailsDto?>
{
    public Task<TagDetailsDto?> Handle(GetTagByIdRequest request, CancellationToken cancellationToken) =>
        db.Tags.AsNoTracking()
            .Where(tag => tag.Id == request.Id)
            .Select(tag => new TagDetailsDto(
                tag.Id,
                tag.Aliases.OrderBy(alias => alias.AddedDateTime).ThenBy(alias => alias.Id)
                    .Select(alias => alias.Name).FirstOrDefault() ?? "Unnamed tag",
                tag.Content,
                tag.Aliases.OrderBy(alias => alias.AddedDateTime).ThenBy(alias => alias.Id)
                    .Select(alias => new TagAliasDto(alias.Id, alias.Name)).ToList()))
            .SingleOrDefaultAsync(cancellationToken);
}
