using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Accord.Domain;

public static class AccordContextExtensions
{
    public static IServiceCollection AddDatabase(this IServiceCollection collection, string connectionString)
    {
        return collection.AddDbContext<AccordContext>(x => x.UseNpgsql(connectionString));
    }

    public static async Task Migrate(AccordContext context)
    {
        await context.Database.MigrateAsync();
    }
}