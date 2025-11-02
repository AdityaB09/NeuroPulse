namespace NeuroPulse.Api.Data;

using Microsoft.EntityFrameworkCore;
using NeuroPulse.Api.Models;

public class AppDb : DbContext
{
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<SearchHit> SearchHits => Set<SearchHit>();

    public AppDb(DbContextOptions<AppDb> opts) : base(opts) { }

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.HasPostgresExtension("vector");

        mb.Entity<Document>(e =>
        {
            e.ToTable("documents");
            e.HasKey(d => d.Id);

            e.Property(d => d.Id).HasColumnName("id");
            e.Property(d => d.Source).HasColumnName("source");
            e.Property(d => d.Content).HasColumnName("content");
            e.Property(d => d.CreatedAt).HasColumnName("created_at");
            e.Property(d => d.Embedding)
             .HasColumnName("embedding")
             .HasColumnType("vector(768)");

            // Now we DO map the column
            e.Property(d => d.ContentSha).HasColumnName("content_sha");
            e.HasIndex(d => d.ContentSha).IsUnique(); // matches the DB unique index
        });

        mb.Entity<SearchHit>(e =>
        {
            e.HasNoKey();
            e.ToView(null);
            e.Property(h => h.Id).HasColumnName("id");
            e.Property(h => h.Source).HasColumnName("source");
            e.Property(h => h.Content).HasColumnName("content");
            e.Property(h => h.Score).HasColumnName("score");
        });
    }
}
