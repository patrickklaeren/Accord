using System;
using System.Linq;

namespace Accord.Services.Helpers;

public static class DateTimeHelper
{
    public static DateTimeOffset Max(params DateTimeOffset[] inputs)
    {
        return inputs.Max();
    }
}