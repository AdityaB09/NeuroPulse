using Pgvector;

public record IngestRequest(string Source, string Content);
public record QueryRequest(string Query, int? TopK);

public class Document
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Source { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public Vector? Embedding { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

// Models.cs
public class SearchRow
{
    public Guid Id { get; set; }
    public string Source { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public double Score { get; set; }
}
