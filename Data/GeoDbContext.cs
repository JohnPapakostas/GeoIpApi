using GeoIpApi.Models;
using Microsoft.EntityFrameworkCore;

namespace GeoIpApi.Data
{
    public class GeoDbContext : DbContext
    {

        public GeoDbContext(DbContextOptions<GeoDbContext> options) : base(options)
        {
        }

        public DbSet<Batch> Batches => Set<Batch>();
        public DbSet<BatchItem> BatchItems => Set<BatchItem>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Batch>(e =>
            {
                e.HasKey(x => x.Id);
            });

            modelBuilder.Entity<BatchItem>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.Ip).HasMaxLength(64);
                e.Property(x => x.Status).HasMaxLength(32);
                e.HasOne(x => x.Batch)
                  .WithMany(b => b.Items)
                  .HasForeignKey(x => x.BatchId);
            });
        }
    }
}
