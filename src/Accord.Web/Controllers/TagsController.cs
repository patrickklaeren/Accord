using System.Threading;
using System.Threading.Tasks;
using Accord.Domain.Model;
using Accord.Services.Tags;
using Accord.Web.DiscordServices;
using Accord.Web.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Accord.Web.Controllers;

[Route("tags")]
public sealed class TagsController(IMediator mediator, DiscordUserService currentUser) : AccordController(mediator, currentUser)
{
    [HttpGet("")]
    public async Task<IActionResult> Index(string? search, int page, CancellationToken ct)
    {
        var user = await GetPermissionUser(ct);
        var canManage = await HasPermission(user, PermissionType.ManageTags, ct);
        var tags = await Mediator.Send(new GetTagsRequest(Page: page, Search: search), ct);
        return View(new TagsViewModel(tags, canManage));
    }

    [HttpGet("create")]
    public async Task<IActionResult> Create(CancellationToken ct)
    {
        if (!await HasPermission(await GetPermissionUser(ct), PermissionType.ManageTags, ct))
            return AccessDenied("You need the Manage Tags permission to create tags.");
        return View(new TagInputModel());
    }

    [HttpPost("create")]
    public async Task<IActionResult> Create(TagInputModel input, CancellationToken ct)
    {
        var user = await GetPermissionUser(ct);
        if (!await HasPermission(user, PermissionType.ManageTags, ct))
            return AccessDenied("You need the Manage Tags permission to create tags.");
        if (!ModelState.IsValid)
            return View(input);

        var result = await Mediator.Send(new AddTagRequest(input.Name.Trim(), input.Content.Trim(), user!), ct);
        if (result.Failure)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage);
            return View(input);
        }
        TempData["Success"] = "Tag created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("{id:int}/edit")]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        if (!await HasPermission(await GetPermissionUser(ct), PermissionType.ManageTags, ct))
            return AccessDenied("You need the Manage Tags permission to edit tags.");
        var tag = await Mediator.Send(new GetTagByIdRequest(id), ct);
        return tag is null ? NotFound() : View(new EditTagViewModel(tag.Id, tag.Name, new TagContentInputModel { Content = tag.Content }, tag.Aliases));
    }

    [HttpPost("{id:int}/edit")]
    public async Task<IActionResult> Edit(int id, [Bind(Prefix = "Input")] TagContentInputModel input, CancellationToken ct)
    {
        var user = await GetPermissionUser(ct);
        if (!await HasPermission(user, PermissionType.ManageTags, ct))
            return AccessDenied("You need the Manage Tags permission to edit tags.");
        var tag = await Mediator.Send(new GetTagByIdRequest(id), ct);
        if (tag is null)
            return NotFound();
        if (ModelState.IsValid)
        {
            var result = await Mediator.Send(new UpdateTagRequest(id, input.Content, user!), ct);
            if (result.Success)
            {
                TempData["Success"] = "Tag updated.";
                return RedirectToAction(nameof(Index));
            }
            ModelState.AddModelError(string.Empty, result.ErrorMessage);
        }
        return View(new EditTagViewModel(id, tag.Name, input, tag.Aliases));
    }

    [HttpPost("{id:int}/aliases")]
    public async Task<IActionResult> AddAlias(int id, TagAliasInputModel input, CancellationToken ct)
    {
        var user = await GetPermissionUser(ct);
        if (!await HasPermission(user, PermissionType.ManageTags, ct))
            return AccessDenied("You need the Manage Tags permission to manage aliases.");
        if (!ModelState.IsValid)
            return InvalidForm(nameof(Edit), new { id });

        var result = await Mediator.Send(new AddTagAliasRequest(id, input.Name, user!), ct);
        return Saved(result, nameof(Edit), "Alias added.", new { id });
    }

    [HttpPost("{id:int}/aliases/{aliasId:int}/rename")]
    public async Task<IActionResult> RenameAlias(int id, int aliasId, TagAliasInputModel input, CancellationToken ct)
    {
        var user = await GetPermissionUser(ct);
        if (!await HasPermission(user, PermissionType.ManageTags, ct))
            return AccessDenied("You need the Manage Tags permission to manage aliases.");
        if (!ModelState.IsValid)
            return InvalidForm(nameof(Edit), new { id });

        var result = await Mediator.Send(new RenameTagAliasRequest(id, aliasId, input.Name, user!), ct);
        return Saved(result, nameof(Edit), "Alias updated.", new { id });
    }

    [HttpPost("{id:int}/aliases/{aliasId:int}/delete")]
    public async Task<IActionResult> RemoveAlias(int id, int aliasId, CancellationToken ct)
    {
        var user = await GetPermissionUser(ct);
        if (!await HasPermission(user, PermissionType.ManageTags, ct))
            return AccessDenied("You need the Manage Tags permission to manage aliases.");

        var result = await Mediator.Send(new RemoveTagAliasRequest(id, aliasId, user!), ct);
        return Saved(result, nameof(Edit), "Alias removed.", new { id });
    }

    [HttpGet("{id:int}/delete")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        if (!await HasPermission(await GetPermissionUser(ct), PermissionType.ManageTags, ct))
            return AccessDenied("You need the Manage Tags permission to delete tags.");
        var tag = await Mediator.Send(new GetTagByIdRequest(id), ct);
        return tag is null ? NotFound() : View(tag);
    }

    [HttpPost("{id:int}/delete")]
    public async Task<IActionResult> ConfirmDelete(int id, CancellationToken ct)
    {
        var user = await GetPermissionUser(ct);
        if (!await HasPermission(user, PermissionType.ManageTags, ct))
            return AccessDenied("You need the Manage Tags permission to delete tags.");
        var tag = await Mediator.Send(new GetTagByIdRequest(id), ct);
        if (tag is null)
            return NotFound();
        var result = await Mediator.Send(new DeleteTagRequest(tag.Name, user!), ct);
        return Saved(result, nameof(Index), "Tag deleted.");
    }
}
