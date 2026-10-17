using System.Threading;
using System.Threading.Tasks;
using Accord.Services.Permissions;
using MediatR;

namespace Accord.Services.Tags;

public sealed record AddTagAliasRequest(int TagId, string Name, PermissionUser User) : IRequest<ServiceResponse>;
public sealed record RenameTagAliasRequest(int TagId, int AliasId, string Name, PermissionUser User) : IRequest<ServiceResponse>;
public sealed record RemoveTagAliasRequest(int TagId, int AliasId, PermissionUser User) : IRequest<ServiceResponse>;

public sealed class ManageTagAliasesHandler(TagService tagService)
    : IRequestHandler<AddTagAliasRequest, ServiceResponse>,
      IRequestHandler<RenameTagAliasRequest, ServiceResponse>,
      IRequestHandler<RemoveTagAliasRequest, ServiceResponse>
{
    public Task<ServiceResponse> Handle(AddTagAliasRequest request, CancellationToken cancellationToken) =>
        tagService.AddAlias(request.TagId, request.Name, request.User, cancellationToken);

    public Task<ServiceResponse> Handle(RenameTagAliasRequest request, CancellationToken cancellationToken) =>
        tagService.RenameAlias(request.TagId, request.AliasId, request.Name, request.User, cancellationToken);

    public Task<ServiceResponse> Handle(RemoveTagAliasRequest request, CancellationToken cancellationToken) =>
        tagService.DeleteAlias(request.TagId, request.AliasId, request.User, cancellationToken);
}
