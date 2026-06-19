using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Accord.Domain;
using Accord.Domain.Model;
using Accord.Services.Permissions;
using LazyCache;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services.Tags;

[RegisterScoped]
public class TagService(AccordContext db, IAppCache appCache, UserPermissionService userPermissionService)
{
    public async Task<string?> GetTag(string name)
    {
        return await appCache.GetOrAddAsync(BuildGetTagCacheKey(name),
            async () =>
            {
                return await db.TagAliases
                    .Where(x => EF.Functions.ILike(x.Name, name))
                    .Select(x => x.Tag!.Content)
                    .SingleOrDefaultAsync();
            },
            DateTimeOffset.UtcNow.AddDays(30));
    }

    public async Task IncreaseTagUsage(string name)
    {
        var tag = await db.TagAliases
            .Where(x => EF.Functions.ILike(x.Name, name))
            .Select(x => x.Tag)
            .SingleOrDefaultAsync();

        if (tag is null)
            return;

        tag.Uses++;

        await db.SaveChangesAsync();
    }

    private async Task<bool> TagExists(string name)
    {
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
        };

        var alias = new TagAlias
        {
            Name = name,
            Tag = tag,
            AddedByUserId = user.DiscordUserId,
            AddedDateTime = DateTimeOffset.UtcNow,
        };

        db.Add(tag);
        db.Add(alias);

        await db.SaveChangesAsync();

        InvalidateCache(name);

        return ServiceResponse.Ok();
    }

    public async Task<ServiceResponse> UpdateTag(string name, string content, PermissionUser user)
    {
        var tag = await db.TagAliases
            .Where(x => EF.Functions.ILike(x.Name, name))
            .Select(x => x.Tag)
            .SingleOrDefaultAsync();

        if (tag is null)
            return ServiceResponse.Fail("Tag not found");

        var canEdit = await CanModifyTag(tag, user);
        if (!canEdit)
            return ServiceResponse.Fail("Missing permission to edit tag");

        tag.Content = content;
        await db.SaveChangesAsync();

        await InvalidateCacheForTag(tag.Id);

        return ServiceResponse.Ok();
    }

    public async Task<ServiceResponse> DeleteTag(string name, PermissionUser user)
    {
        var tag = await db.TagAliases
            .Where(x => EF.Functions.ILike(x.Name, name))
            .Select(x => x.Tag)
            .SingleOrDefaultAsync();

        if (tag is null)
            return ServiceResponse.Fail("Tag not found");

        var canDelete = await CanModifyTag(tag, user);
        
        if (!canDelete)
            return ServiceResponse.Fail("Missing permission to delete tag");
        
        await InvalidateCacheForTag(tag.Id);

        await db.TagAliases
            .Where(x => x.TagId == tag.Id)
            .ExecuteDeleteAsync();
        
        db.Remove(tag);

        await db.SaveChangesAsync();

        return ServiceResponse.Ok();
    }

    public async Task<ServiceResponse> AddAlias(string name, string newAlias, PermissionUser user)
    {
        var tag = await db.TagAliases
            .Where(x => EF.Functions.ILike(x.Name, name))
            .Select(x => x.Tag)
            .SingleOrDefaultAsync();

        if (tag is null)
            return ServiceResponse.Fail("Tag not found");

        var canAdd = await CanModifyTag(tag, user);
        if (!canAdd)
            return ServiceResponse.Fail("Missing permission to add alias");

        var aliasExists = await TagExists(newAlias);
        if (aliasExists)
            return ServiceResponse.Fail("Alias already exists");

        var alias = new TagAlias
        {
            Name = newAlias,
            TagId = tag.Id,
            AddedByUserId = user.DiscordUserId,
            AddedDateTime = DateTimeOffset.UtcNow,
        };

        db.Add(alias);
        await db.SaveChangesAsync();

        return ServiceResponse.Ok();
    }

    public async Task<ServiceResponse> DeleteAlias(string name, PermissionUser user)
    {
        var tagAlias = await db.TagAliases
            .Include(x => x.Tag)
            .Where(x => EF.Functions.ILike(x.Name, name))
            .SingleOrDefaultAsync();

        if (tagAlias is null)
            return ServiceResponse.Fail("Alias not found");

        var canDelete = await CanModifyTag(tagAlias.Tag!, user);
        if (!canDelete)
            return ServiceResponse.Fail("Missing permission to delete alias");

        db.Remove(tagAlias);
        await db.SaveChangesAsync();

        InvalidateCache(name);

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

    private async Task<bool> CanModifyTag(Tag tag, PermissionUser user)
    {
        if (user.IsAdministrator)
            return true;

        if (tag.AddedByUserId == user.DiscordUserId)
            return true;

        return await userPermissionService.HasPermission(user, PermissionType.ManageTags);
    }

    private async Task InvalidateCacheForTag(int tagId)
    {
        var aliases = await db.TagAliases
            .Where(x => x.TagId == tagId)
            .Select(x => x.Name)
            .ToListAsync();

        foreach (var alias in aliases)
        {
            InvalidateCache(alias);
        }
    }

    private void InvalidateCache(string name)
    {
        appCache.Remove(BuildGetTagCacheKey(name));
    }

    private static string BuildGetTagCacheKey(string name)
    {
        return $"{nameof(TagService)}/{nameof(GetTag)}/{name.ToLowerInvariant()}";
    }
}

public sealed record TagSearchResult(string Name, string Content);