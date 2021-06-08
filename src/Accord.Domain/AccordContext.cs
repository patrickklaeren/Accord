using Accord.Domain.Model;
using Microsoft.EntityFrameworkCore;

namespace Accord.Domain
{
    public class AccordContext : DbContext
    {
        public AccordContext(DbContextOptions<AccordContext> builderOptions) : base(builderOptions)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AccordContext).Assembly);
            base.OnModelCreating(modelBuilder);
        }

        public virtual DbSet<User> Users { get; set; }
        public virtual DbSet<ChannelFlag> ChannelFlags { get; set; }
    }
}
