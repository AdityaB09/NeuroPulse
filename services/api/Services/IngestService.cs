namespace NeuroPulse.Api.Services;

using Microsoft.EntityFrameworkCore;
using NeuroPulse.Api.Data;
using NeuroPulse.Api.Models;
using Npgsql;
using System.Security.Cryptography;
using System.Text;

public class IngestService
{
    private readonly AppDb _db;
    private readonly EmbeddingClient _emb;

    public IngestService(AppDb db, EmbeddingClient emb)
    {
        _db = db;
        _emb = emb;
    }

    public async Task<Document> IngestAsync(IngestRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Source)) throw new ArgumentException("source required");
        if (string.IsNullOrWhiteSpace(req.Content)) throw new ArgumentException("content required");

        // 1) Compute stable SHA256 of content (add Source if you want per-source uniqueness)
        var sha = Sha256Hex(req.Content);

        // 2) Quick exist check (fast because of UNIQUE index)
        var existing = await _db.Documents.AsNoTracking()
            .FirstOrDefaultAsync(d => d.ContentSha == sha);

        if (existing is not null)
            return existing;

        // 3) Create with embedding
        var vec = await _emb.EmbedAsync(req.Content);

        var doc = new Document
        {
            Source = req.Source,
            Content = req.Content,
            ContentSha = sha,
            Embedding = new Pgvector.Vector(vec)
        };

        _db.Documents.Add(doc);

        try
        {
            await _db.SaveChangesAsync();
            return doc;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg && pg.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            // Another writer beat us; fetch and return the existing row
            var winner = await _db.Documents.AsNoTracking()
                .FirstAsync(d => d.ContentSha == sha);
            return winner;
        }
    }

    private static string Sha256Hex(string s)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(s));
        var sb = new StringBuilder(bytes.Length * 2);
        foreach (var b in bytes) sb.Append(b.ToString("x2"));
        return sb.ToString();
    }
}
