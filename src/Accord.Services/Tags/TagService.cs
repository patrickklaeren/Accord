using System;
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

    private async Task<bool> TagExists(string name)
    {
        return await db.TagAliases.AnyAsync(x => EF.Functions.ILike(x.Name, name));
    }

    public async Task<ServiceResponse> AddTag(string name, string content, ulong discordUserId)
    {
        var exists = await TagExists(name);
        
        if (exists)
            return ServiceResponse.Fail("Tag already exists");

        var tag = new Tag
        {
            Content = content,
            AddedByUserId = discordUserId,
            AddedDateTime = DateTimeOffset.UtcNow,
        };

        var alias = new TagAlias
        {
            Name = name,
            Tag = tag,
            AddedByUserId = discordUserId,
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

public sealed record TagDto(string Content);