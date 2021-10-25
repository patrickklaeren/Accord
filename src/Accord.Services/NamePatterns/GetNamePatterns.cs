using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using Accord.Domain.Model;
using LazyCache;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services.NamePatterns;

public sealed record GetNamePatternsRequest() : IRequest<List<NamePatternDto>>;
public sealed record InvalidateGetNamePatternsRequest : IRequest;

public sealed record NamePatternDto(Regex Pattern, PatternType Type, OnNamePatternDiscovery OnDiscovery);

public class GetNamePatternsHandler : RequestHandler<InvalidateGetNamePatternsRequest>, IRequestHandler<GetNamePatternsRequest, List<NamePatternDto>>
{
    private readonly AccordContext _db;
    private readonly IAppCache _appCache;

    public GetNamePatternsHandler(AccordContext db, IAppCache appCache)
    {
        _db = db;
        _appCache = appCache;
    }

    public async Task<List<NamePatternDto>> Handle(GetNamePatternsRequest request, CancellationToken cancellationToken)
    {
        return await _appCache.GetOrAddAsync(BuildGetNamePatternsCacheKey(),
            GetNamePatterns,
            DateTimeOffset.Now.AddDays(30));
    }

    private static string BuildGetNamePatternsCacheKey()
    {
        return $"{nameof(GetNamePatternsHandler)}/{nameof(GetNamePatterns)}";
    }

    private async Task<List<NamePatternDto>> GetNamePatterns()
    {
        var rawPatterns = await _db.NamePatterns
            .Select(x => new
            {
                x.Pattern,
                x.Type,
                x.OnDiscovery
            }).ToListAsync();

        return rawPatterns
            .Select(x => new NamePatternDto(new Regex(x.Pattern, RegexOptions.IgnoreCase), x.Type, x.OnDiscovery))
            .ToList();
    }

    protected override void Handle(InvalidateGetNamePatternsRequest request)
    {
        _appCache.Remove(BuildGetNamePatternsCacheKey());
    }
}