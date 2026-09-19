using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Accord.Domain;
using Accord.Domain.Model;
using Accord.Services.Permissions;
using Accord.Services.RunOptions;
using LazyCache;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Accord.Services.Tags;

[RegisterScoped]
public partial class TagService(
    AccordContext db,
    IAppCache appCache,
    UserPermissionService userPermissionService,
    ILogger<TagService> logger,
    IServiceScopeFactory scopeFactory,
    RunOptionService runOptionService
)
{
    public async Task<TagDto?> GetTagByName(string name)
    {
        var cached = appCache.GetTagByName(name);
        if (cached is not null)
        {
            return cached;
        }

        LogCacheMissForTagName(name);

        var tag = await db.TagAliases
            .Include(x => x.Tag)
            .ThenInclude(x => x!.Aliases)
            .Where(x => EF.Functions.ILike(x.Name, name))
            .Select(x => ToDto(x.Tag!))
            .SingleOrDefaultAsync();

        if (tag is not null)
        {
            Store(tag);
        }

        return tag;
    }

    private async Task<TagDto?> GetTagById(int id)
    {
        var cached = appCache.GetTagById(id);
        if (cached is not null)
        {
            return cached;
        }

        LogCacheMissForTagId(id);

        var tag = await db.Tags
            .Where(x => x.Id == id)
            .Select(x => ToDto(x)).SingleOrDefaultAsync();

        if (tag is not null)
        {
            Store(tag);
        }

        return tag;
    }

    // Load a tracked Tag entity (including aliases) by any alias name.
    private async Task<Tag?> GetTrackedTagByName(string name)
    {
        return await db.Tags
            .Include(t => t.Aliases)
            .Where(t => t.Aliases.Any(a => EF.Functions.ILike(a.Name, name)))
            .SingleOrDefaultAsync();
    }

    public async Task<string[]> GetTagsContents(string[] names)
    {
        var maxRepliedTagsPerMessage = await runOptionService.GetOption<int>(RunOptionKey.MaxRepliedTagsPerMessage);
        var results = new Dictionary<int, TagDto>();
        foreach (var name in names)
        {
            var tagDto = await GetTagByName(name);
            if (tagDto is null)
            {
                continue;
            }

            results[tagDto.Id] = tagDto;
            if (results.Count >= maxRepliedTagsPerMessage)
            {
                break;
            }
        }

        if (results.Count == 0)
            return [];

        var contents = results.Values.Select(x => x.Content).ToArray();
        foreach (var tagDto in results.Values)
        {
            Store(tagDto with { Uses = tagDto.Uses + 1 });
        }

        var ids = results.Keys.ToArray();
        _ = IncrementUsesAsync(ids);
        return contents;
    }

    private async Task IncrementUsesAsync(int[] ids)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var scopedDb = scope.ServiceProvider.GetRequiredService<AccordContext>();

            await scopedDb.Tags
                .Where(t => ids.AsEnumerable().Contains(t.Id))
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.Uses, t => t.Uses + 1));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to persist tag usage increments for {Ids}", ids);
        }
    }

    private async Task<bool> TagExists(string name)
    {
        if (appCache.GetTagByName(name) is not null)
        {
            return true;
        }

        return await db.TagAliases.AnyAsync(x => EF.Functions.ILike(x.Name, name));
    }

    public async Task<ServiceResponse> AddTag(string name, string content, PermissionUser user)
    {
        var canAdd = await CanAddTag(user);
        if (!canAdd)
            return ServiceResponse.Fail("Missing permission to add tags");

        var exists = await TagExists(name);

        if (exists)
            return ServiceResponse.Fail("Tag already exists");

        var tag = new Tag
        {
            Content = content,
            AddedByUserId = user.DiscordUserId,
            AddedDateTime = DateTimeOffset.UtcNow,
            Aliases =
            [
                new TagAlias
                {
                    Name = name,
                    AddedByUserId = user.DiscordUserId,
                    AddedDateTime = DateTimeOffset.UtcNow,
                }
            ]
        };

        db.Add(tag);
        await db.SaveChangesAsync();

        Store(ToDto(tag));
        return ServiceResponse.Ok();
    }

    public async Task<ServiceResponse> UpdateTag(int id, string content, PermissionUser user)
    {
        var existing = await GetTagById(id);

        if (existing is null)
        {
            return ServiceResponse.Fail("Tag not found");
        }

        var canEdit = await CanModifyTag(existing.AddedByDiscordUserId, user);
        if (!canEdit)
        {
            return ServiceResponse.Fail("Missing permission to edit tag");
        }

        var tag = await db.Tags
            .Include(t => t.Aliases)
            .Where(t => t.Id == id)
            .SingleOrDefaultAsync();

        if (tag is null)
        {
            return ServiceResponse.Fail("Tag not found");
        }

        Evict(existing);

        tag.Content = content;
        await db.SaveChangesAsync();

        Store(ToDto(tag));

        return ServiceResponse.Ok();
    }

    public async Task<ServiceResponse> DeleteTag(string name, PermissionUser user)
    {
        var tagEntity = await GetTrackedTagByName(name);

        if (tagEntity is null)
        {
            return ServiceResponse.Fail("Tag not found");
        }

        var canDelete = await CanModifyTag(tagEntity.AddedByUserId, user);
        if (!canDelete)
        {
            return ServiceResponse.Fail("Missing permission to delete tag");
        }

        Evict(ToDto(tagEntity));

        db.Remove(tagEntity);

        await db.SaveChangesAsync();

        return ServiceResponse.Ok();
    }

    public async Task<ServiceResponse> AddAlias(string name, string newAlias, PermissionUser user)
    {
        var tagEntity = await GetTrackedTagByName(name);

        if (tagEntity is null)
        {
            return ServiceResponse.Fail("Tag not found");
        }

        var canAdd = await CanModifyTag(tagEntity.AddedByUserId, user);
        if (!canAdd)
        {
            return ServiceResponse.Fail("Missing permission to add alias");
        }

        var aliasExists = await TagExists(newAlias);
        if (aliasExists)
        {
            return ServiceResponse.Fail("Alias already exists");
        }

        Evict(ToDto(tagEntity));

        var alias = new TagAlias
        {
            Name = newAlias,
            TagId = tagEntity.Id,
            AddedByUserId = user.DiscordUserId,
            AddedDateTime = DateTimeOffset.UtcNow,
        };

        tagEntity.Aliases.Add(alias);
        await db.SaveChangesAsync();

        Store(ToDto(tagEntity));

        return ServiceResponse.Ok();
    }

    public async Task<ServiceResponse> DeleteAlias(string name, PermissionUser user)
    {
        var tagEntity = await GetTrackedTagByName(name);

        if (tagEntity is null)
        {
            return ServiceResponse.Fail("Tag not found");
        }

        var canDelete = await CanModifyTag(tagEntity.AddedByUserId, user);
        if (!canDelete)
        {
            return ServiceResponse.Fail("Missing permission to delete alias");
        }

        var alias = tagEntity.Aliases.FirstOrDefault(a => string.Equals(a.Name, name, StringComparison.OrdinalIgnoreCase));

        if (alias is null)
        {
            return ServiceResponse.Fail("Alias not found");
        }

        Evict(ToDto(tagEntity));

        tagEntity.Aliases.Remove(alias);
        db.Remove(alias);
        await db.SaveChangesAsync();

        Store(ToDto(tagEntity));

        return ServiceResponse.Ok();
    }

    public async Task<List<TagSearchResult>> SearchTags(string searchTerm)
    {
        return await db.Tags
            .Where(t => t.Aliases.Any(a => EF.Functions.ILike(a.Name, $"%{searchTerm}%")))
            .Select(t => new TagSearchResult(
                t.Aliases.OrderBy(a => a.AddedDateTime).Select(a => a.Name).First(),
                t.Content
            ))
            .ToListAsync();
    }

    private async Task<bool> CanAddTag(PermissionUser user)
    {
        if (user.IsAdministrator)
            return true;

        return await userPermissionService.HasPermission(user, PermissionType.ManageTags);
    }

    private async Task<bool> CanModifyTag(ulong addedByUserId, PermissionUser user)
    {
        if (user.IsAdministrator)
            return true;

        if (addedByUserId == user.DiscordUserId)
            return true;

        return await userPermissionService.HasPermission(user, PermissionType.ManageTags);
    }

    private void Store(TagDto tagDto)
    {
        appCache.StoreTag(tagDto);
        foreach (var alias in tagDto.Aliases)
        {
            appCache.StoreTagAlias(alias, tagDto.Id);
        }
    }

    private void Evict(TagDto tagDto)
    {
        appCache.RemoveTagById(tagDto.Id);
        foreach (var alias in tagDto.Aliases)
        {
            appCache.RemoveTagIdByName(alias);
        }
    }

    private static TagDto ToDto(Tag tag)
    {
        return new TagDto(
            tag.Id,
            [.. tag.Aliases.Select(x => x.Name)],
            tag.Uses,
            tag.Content,
            tag.AddedDateTime,
            tag.AddedByUserId
        );
    }

    [LoggerMessage(LogLevel.Information, "Cache miss for tag name: {TagName}. Querying database.")]
    partial void LogCacheMissForTagName(string tagName);

    [LoggerMessage(LogLevel.Information, "Cache miss for tag id: {TagId}. Querying database.")]
    partial void LogCacheMissForTagId(int tagId);
}

public sealed record TagSearchResult(string Name, string Content);

public sealed record TagDto(
    int Id,
    IReadOnlyCollection<string> Aliases,
    int Uses,
    string Content,
    DateTimeOffset AddedDateTime,
    ulong AddedByDiscordUserId
);