using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using Accord.Domain.Model;
using Accord.Services.Permissions;
using Accord.Services.RunOptions;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services.Tags;

[RegisterScoped]
public class TagService(
    AccordContext db,
    TagCache tagCache,
    UserPermissionService userPermissionService,
    RunOptionService runOptionService
)
{
    public async Task<TagDto?> GetTagByName(string name)
    {
        return await tagCache.GetTagByName(name);
    }

    public async Task<IReadOnlyCollection<string>> GetTagsContents(IReadOnlyCollection<string> names)
    {
        var maxRepliedTagsPerMessage = await runOptionService.GetOption<int>(RunOptionKey.MaxRepliedTagsPerMessage);

        var results = new List<string>();

        foreach (var name in names)
        {
            var tagDto = await GetTagByName(name);

            if (tagDto is null)
            {
                continue;
            }

            await TrackTagUse(tagDto.Id);
            results.Add(tagDto.Content);

            if (results.Count >= maxRepliedTagsPerMessage)
            {
                break;
            }
        }

        return results;
    }

    private async Task TrackTagUse(int id)
    {
        await db.Tags
            .Where(x => x.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.Uses, t => t.Uses + 1));
    }

    private async Task<bool> TagExists(string name, int? excludedAliasId = null, CancellationToken ct = default)
    {
        var pattern = name.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
        return await db.TagAliases.AnyAsync(x => x.Id != excludedAliasId && EF.Functions.ILike(x.Name, pattern, "\\"), ct);
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
        
        tagCache.UncacheTag(name);

        return ServiceResponse.Ok();
    }

    public async Task<ServiceResponse> UpdateTag(int id, string content, PermissionUser user)
    {
        var tag = await db.Tags
            .Include(x => x.Aliases)
            .Where(x => x.Id == id)
            .SingleAsync();

        var canEdit = await CanModifyTag(tag.AddedByUserId, user);
        if (!canEdit)
        {
            return ServiceResponse.Fail("Missing permission to edit tag");
        }

        tag.Content = content;
        await db.SaveChangesAsync();

        foreach (var alias in tag.Aliases)
        {
            tagCache.UncacheTag(alias.Name);
        }

        return ServiceResponse.Ok();
    }

    public async Task<ServiceResponse> DeleteTag(string name, PermissionUser user)
    {
        var tag = await db.Tags
            .Include(x => x.Aliases)
            .Where(x => x.Aliases.Any(d => d.Name == name))
            .SingleOrDefaultAsync();

        if (tag is null)
        {
            return ServiceResponse.Fail("Tag not found");
        }

        var canDelete = await CanModifyTag(tag.AddedByUserId, user);
        if (!canDelete)
        {
            return ServiceResponse.Fail("Missing permission to delete tag");
        }

        db.Remove(tag);
        await db.SaveChangesAsync();

        foreach (var alias in tag.Aliases)
        {
            tagCache.UncacheTag(alias.Name);
        }

        return ServiceResponse.Ok();
    }

    public async Task<ServiceResponse> AddAlias(string name, string newAlias, PermissionUser user)
    {
        var tag = await db.Tags
            .Include(x => x.Aliases)
            .Where(x => x.Aliases.Any(d => d.Name == name))
            .SingleOrDefaultAsync();

        return await AddAlias(tag, newAlias, user, CancellationToken.None);
    }

    public async Task<ServiceResponse> AddAlias(int tagId, string newAlias, PermissionUser user, CancellationToken ct)
    {
        var tag = await db.Tags.Include(tag => tag.Aliases).SingleOrDefaultAsync(tag => tag.Id == tagId, ct);
        return await AddAlias(tag, newAlias, user, ct);
    }

    private async Task<ServiceResponse> AddAlias(Tag? tag, string newAlias, PermissionUser user, CancellationToken ct)
    {
        if (tag is null)
            return ServiceResponse.Fail("Tag not found");
        var canAdd = await CanModifyTag(tag.AddedByUserId, user);
        if (!canAdd)
        {
            return ServiceResponse.Fail("Missing permission to add alias");
        }

        newAlias = newAlias.Trim();
        if (string.IsNullOrWhiteSpace(newAlias) || newAlias.Length > 100)
            return ServiceResponse.Fail("Enter an alias between 1 and 100 characters.");

        var aliasExists = await TagExists(newAlias, ct: ct);
        if (aliasExists)
        {
            return ServiceResponse.Fail("Alias already exists");
        }

        var alias = new TagAlias
        {
            Name = newAlias,
            TagId = tag.Id,
            AddedByUserId = user.DiscordUserId,
            AddedDateTime = DateTimeOffset.UtcNow,
        };

        db.TagAliases.Add(alias);
        await db.SaveChangesAsync(ct);

        foreach (var name in tag.Aliases.Select(alias => alias.Name).Append(newAlias).Distinct())
            tagCache.UncacheTag(name);

        return ServiceResponse.Ok();
    }

    public async Task<ServiceResponse> RenameAlias(int tagId, int aliasId, string name, PermissionUser user, CancellationToken ct)
    {
        var tag = await db.Tags.Include(tag => tag.Aliases).SingleOrDefaultAsync(tag => tag.Id == tagId, ct);
        if (tag?.Aliases.SingleOrDefault(alias => alias.Id == aliasId) is not { } alias)
            return ServiceResponse.Fail("Alias not found for this tag.");
        if (!await CanModifyTag(tag.AddedByUserId, user))
            return ServiceResponse.Fail("Missing permission to edit alias");

        name = name.Trim();
        if (string.IsNullOrWhiteSpace(name) || name.Length > 100)
            return ServiceResponse.Fail("Enter an alias between 1 and 100 characters.");
        if (await TagExists(name, aliasId, ct))
            return ServiceResponse.Fail("Alias already exists");

        var previousName = alias.Name;
        alias.Name = name;
        await db.SaveChangesAsync(ct);

        foreach (var aliasName in tag.Aliases.Select(alias => alias.Name).Append(previousName).Distinct())
            tagCache.UncacheTag(aliasName);

        return ServiceResponse.Ok();
    }

    public async Task<ServiceResponse> DeleteAlias(string name, PermissionUser user)
    {
        var tag = await db.Tags
            .Include(x => x.Aliases)
            .Where(x => x.Aliases.Any(d => d.Name == name))
            .SingleOrDefaultAsync();

        return await DeleteAlias(tag, tag?.Aliases.Single(alias => alias.Name == name), user, CancellationToken.None);
    }

    public async Task<ServiceResponse> DeleteAlias(int tagId, int aliasId, PermissionUser user, CancellationToken ct)
    {
        var tag = await db.Tags.Include(tag => tag.Aliases).SingleOrDefaultAsync(tag => tag.Id == tagId, ct);
        return await DeleteAlias(tag, tag?.Aliases.SingleOrDefault(alias => alias.Id == aliasId), user, ct);
    }

    private async Task<ServiceResponse> DeleteAlias(Tag? tag, TagAlias? targetAlias, PermissionUser user, CancellationToken ct)
    {
        if (tag is null || targetAlias is null)
            return ServiceResponse.Fail("Alias not found for this tag.");
        var canDelete = await CanModifyTag(tag.AddedByUserId, user);
        if (!canDelete)
        {
            return ServiceResponse.Fail("Missing permission to delete alias");
        }

        if (tag.Aliases.Count <= 1)
            return ServiceResponse.Fail("A tag must keep at least one alias. Rename it or delete the tag instead.");

        var aliases = tag.Aliases.Select(alias => alias.Name).ToArray();
        db.TagAliases.Remove(targetAlias);
        await db.SaveChangesAsync(ct);

        foreach (var aliasName in aliases)
            tagCache.UncacheTag(aliasName);

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
