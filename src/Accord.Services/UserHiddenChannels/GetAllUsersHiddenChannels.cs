using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using Accord.Domain.Model;
using LazyCache;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services.UserHiddenChannels;

public sealed record GetAllUsersHiddenChannelsRequest() : IRequest<List<UserHiddenChannel>>;

[AutoConstructor]
public partial class GetAllUsersHiddenChannels :
    IRequestHandler<GetAllUsersHiddenChannelsRequest, List<UserHiddenChannel>>
{
    private readonly AccordContext _db;

    public async Task<List<UserHiddenChannel>> Handle(GetAllUsersHiddenChannelsRequest request, CancellationToken cancellationToken)
    {
        return await _db.UserHiddenChannels
            .AsNoTracking()
            .ToListAsync(cancellationToken: cancellationToken); }
}