using Microsoft.EntityFrameworkCore;
using Pgvector;

namespace NeuroPulse.Api;

public class AppDb : DbContext
{
    public DbSet<Document> Documents => Set<Document>();
    public AppDb(DbContextOptions<AppDb> opts) : base(opts) { }

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.HasPostgresExtension("vector");

        mb.Entity<Document>(e =>
        {
            e.ToTable("documents");
            e.HasKey(d => d.Id);
            // Stay consistent with your SQL (vector(768))
            e.Property(d => d.Embedding).HasColumnType("vector(768)");
        });
    }
}
