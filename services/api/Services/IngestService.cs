public class IngestService
{
    private readonly AppDb _db;
    private readonly EmbeddingClient _emb;
    public IngestService(AppDb db, EmbeddingClient emb) { _db = db; _emb = emb; }

    public async Task<Document> IngestAsync(IngestRequest req)
    {
        var vec = await _emb.EmbedAsync(req.Content);
        var doc = new Document
        {
            Source = req.Source,
            Content = req.Content,
            Embedding = new Pgvector.Vector(vec)
        };
        _db.Documents.Add(doc);
        await _db.SaveChangesAsync();
        return doc;
    }
}
