using System.Linq;
using System.Threading.Tasks;
using Accord.Domain;
using LazyCache;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services.Tags;

[RegisterScoped]
public class TagCache(AccordContext db, IAppCache appCache)
{
    public async Task<TagDto?> GetTagByName(string name)
    {
        return await appCache.GetOrAddAsync(BuildTagAliasCacheKey(name), GetData);

        async Task<TagDto?> GetData()
        {
            return await db.Tags
                .Where(x => x.Aliases.Any(d => d.Name == name))
                .Select(x => new TagDto
                (
                    x.Id,
                    x.Aliases.Select(d => d.Name).ToList(),
                    x.Uses,
                    x.Content,
                    x.AddedDateTime,
                    x.AddedByUserId
                )).SingleOrDefaultAsync();
        }
    }

    public void UncacheTag(string name)
    {
        appCache.Remove(BuildTagAliasCacheKey(name));
    }

    private static string BuildTagAliasCacheKey(string name)
    {
        return $"{nameof(TagCache)}/{nameof(TagService)}/{name.ToLowerInvariant()}";
    }
}