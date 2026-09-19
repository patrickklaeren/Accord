
using LazyCache;

namespace Accord.Services.Tags;

public static class TagCache
{
    private static string BuildTagAliasCacheKey(string name)
    {
        return $"{nameof(TagCache)}/{nameof(TagService)}/{name.ToLowerInvariant()}";
    }

    private static string BuildTagIdCacheKey(int id)
    {
        return $"{nameof(TagCache)}/{nameof(TagService)}/{id}";
    }

    extension(IAppCache appCache)
    {
        public TagDto? GetTagById(int id)
        {
            return appCache.Get<TagDto?>(BuildTagIdCacheKey(id));
        }

        public TagDto? GetTagByName(string name)
        {
            var cached = appCache.Get<int?>(BuildTagAliasCacheKey(name));
            return cached is not null ? appCache.GetTagById(cached.Value) : null;
        }

        public void StoreTagAlias(string name, int id)
        {
            appCache.Add(BuildTagAliasCacheKey(name), id);
        }

        public void StoreTag(TagDto tagDto)
        {
            appCache.Add(BuildTagIdCacheKey(tagDto.Id), tagDto);
        }

        public void RemoveTagIdByName(string name)
        {
            appCache.Remove(BuildTagAliasCacheKey(name));
        }

        public void RemoveTagById(int id)
        {
            appCache.Remove(BuildTagIdCacheKey(id));
        }
    }
}