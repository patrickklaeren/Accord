using System;
using System.Linq;
using System.Threading.Tasks;
using Accord.Domain;
using Accord.Domain.Model;
using LazyCache;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services.RunOptions;

[RegisterScoped]
public class RunOptionService(AccordContext db, IAppCache appCache)
{
    public async Task<T> GetOption<T>(RunOptionKey key)
    {
        return await appCache.GetOrAddAsync(
            BuildGetOptionCacheKey(key),
            () => GetOptionUncached<T>(key));
    }

    private static string BuildGetOptionCacheKey(RunOptionKey key)
    {
        return $"{nameof(RunOptionService)}/{nameof(GetOption)}/{key}";
    }

    private async Task<T> GetOptionUncached<T>(RunOptionKey key)
    {
        var value = await db.RunOptions
            .Where(x => x.Key == key)
            .Select(x => x.Value)
            .SingleAsync();

        if (string.IsNullOrWhiteSpace(value))
            return default!;
        
        var convertedValue = (T)Convert.ChangeType(value, typeof(T));
        return convertedValue;
    }

    public Task UpdateOption<T>(RunOptionKey key, T? value)
    {
        return UpdateOption(key, value?.ToString());
    }
    
    public async Task UpdateOption(RunOptionKey key, string? value)
    {
        var configuration = await db.RunOptions
            .Where(x => x.Key == key)
            .SingleAsync();

        if (value is null)
        {
            configuration.Value = string.Empty;
        }
        else
        {
            configuration.Value = configuration.Type switch
            {
                RunOptionType.String => value,
                
                RunOptionType.Integer => int.TryParse(value, out _)
                    ? value
                    : throw new InvalidOperationException("Invalid integer"),
                
                RunOptionType.Boolean => bool.TryParse(value, out _)
                    ? value
                    : throw new InvalidOperationException("Invalid boolean"),
                
                RunOptionType.DateTime => DateTime.TryParse(value, out _)
                    ? value
                    : throw new InvalidOperationException("Invalid DateTime"),
                
                RunOptionType.ULong => ulong.TryParse(value, out _)
                    ? value
                    : throw new InvalidOperationException("Invalid ULong"),
                
                _ => throw new InvalidOperationException("Configuration type not configured")
            };
        }
        
        await db.SaveChangesAsync();
        appCache.Remove(BuildGetOptionCacheKey(key));
    }
}