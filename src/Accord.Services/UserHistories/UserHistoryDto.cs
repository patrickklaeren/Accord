using System;
using Accord.Domain.Model;

namespace Accord.Services.UserHistories;

public sealed record UserHistoryDto(int Id, 
    UserHistoryType Type, 
    string Content,
    DateTimeOffset AddedDateTime,
    ulong TargetUserId,
    ulong AddedByUserId);