namespace NeuroPulse.Api.Models;

using Pgvector;

public record IngestRequest(string Source, string Content);
public record QueryRequest(string Query, int? TopK, int? Offset, int? Limit);

// DB entity
public class Document
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Source { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public Vector? Embedding { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Optional (used only if column exists in DB)
    public string? ContentSha { get; set; }
}

// Keyless projection for search results
public class SearchHit
{
    public Guid Id { get; set; }
    public string Source { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public double Score { get; set; }
}
