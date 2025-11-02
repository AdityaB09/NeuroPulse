using Microsoft.EntityFrameworkCore;

public class VectorSearch
{
    private readonly AppDb _db;
    private readonly EmbeddingClient _emb;
    public VectorSearch(AppDb db, EmbeddingClient emb) { _db = db; _emb = emb; }

    public async Task<object[]> SearchAsync(string query, int topK)
{
    var qvec = await _emb.EmbedAsync(query);

    var sql = @"
        SELECT id, source, content,
               1 - (embedding <=> CAST(@p0 AS vector)) AS score
        FROM documents
        WHERE embedding IS NOT NULL
        ORDER BY embedding <-> CAST(@p0 AS vector)
        LIMIT @p1";

    var rows = await _db.Set<SearchRow>().FromSqlRaw(sql, qvec, topK).ToListAsync();
    return rows.Select(r => new { r.Id, r.Source, r.Content }).ToArray();
}

}
