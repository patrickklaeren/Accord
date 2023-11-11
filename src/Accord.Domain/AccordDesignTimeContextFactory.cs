using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Accord.Domain;

public class AccordDesignTimeContextFactory : IDesignTimeDbContextFactory<AccordContext>
{
    AccordContext IDesignTimeDbContextFactory<AccordContext>.CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<AccordContext>();
        builder.UseNpgsql("Host=127.0.0.1;Username=postgres;Password=password;Database=Accord-Dev");
        return new AccordContext(builder.Options);
    }
}