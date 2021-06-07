using Accord.Domain.Model;
using Microsoft.EntityFrameworkCore;

namespace Accord.Domain
{
    public class AccordContext : DbContext
    {
        public AccordContext(DbContextOptions<AccordContext> builderOptions) : base(builderOptions)
        {
        }

        public virtual DbSet<User> Users { get; set; }
    }
}
