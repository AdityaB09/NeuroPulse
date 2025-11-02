namespace NeuroPulse.Api.Services;

using Microsoft.EntityFrameworkCore;
using NeuroPulse.Api.Data;
using NeuroPulse.Api.Models;

public class VectorSearch
{
    private readonly AppDb _db;
    private readonly EmbeddingClient _emb;

    public VectorSearch(AppDb db, EmbeddingClient emb)
    {
        _db = db;
        _emb = emb;
    }

    public async Task<object[]> SearchAsync(string query, int topK, int offset = 0)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Array.Empty<object>();

        var qvec = await _emb.EmbedAsync(query);

        // Raw SQL into keyless projection to avoid EF trying to read columns we didn't select.
        var sql = @"
            SELECT id, source, content, 1 - (embedding <=> CAST(@p0 AS vector)) AS score
            FROM documents
            WHERE embedding IS NOT NULL
            ORDER BY embedding <-> CAST(@p0 AS vector)
            LIMIT @p1 OFFSET @p2";

        var rows = await _db.Set<SearchHit>()
                            .FromSqlRaw(sql, qvec, topK, offset)
                            .ToListAsync();

        return rows.Select(r => new { r.Id, r.Source, r.Content, r.Score }).ToArray();
    }
}
