using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Accord.Domain
{
    public class AccordDesignTimeContextFactory : IDesignTimeDbContextFactory<AccordContext>
    {
        AccordContext IDesignTimeDbContextFactory<AccordContext>.CreateDbContext(string[] args)
        {
            var builder = new DbContextOptionsBuilder<AccordContext>();

            builder.UseSqlServer("Server=localhost;Database=Accord-Dev;Trusted_Connection=True");

            return new AccordContext(builder.Options);
        }
    }
}
