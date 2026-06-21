using System.Threading;
using System.Threading.Tasks;
using Accord.Services.Permissions;
using MediatR;

namespace Accord.Services.Shlink;

public sealed record ShortenUrlRequest(PermissionUser User, string Url) : IRequest<ServiceResponse<string>>;

public class ShortenUrlHandler(ShlinkApiService shlinkApiService) 
    : IRequestHandler<ShortenUrlRequest, ServiceResponse<string>>
{
    public async Task<ServiceResponse<string>> Handle(ShortenUrlRequest request, CancellationToken cancellationToken)
    {
        return await shlinkApiService.CreateLink(request.User, request.Url);
    }
}