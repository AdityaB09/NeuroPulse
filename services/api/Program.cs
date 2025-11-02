using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Configuration
var dbCxn         = builder.Configuration.GetConnectionString("Db")!;
var redisUrl      = builder.Configuration.GetSection("Redis").GetValue<string>("Url") ?? "redis:6379";
var embeddingsBase= builder.Configuration.GetSection("Embeddings").GetValue<string>("BaseUrl") ?? "http://embeddings:8000";

// Services
builder.Services.AddDbContext<AppDb>(opt =>
{
    // Option B: explicit snake_case mapping in OnModelCreating (no UseSnakeCaseNamingConvention)
    opt.UseNpgsql(dbCxn, npg => npg.UseVector());
});

builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisUrl));

builder.Services.AddHttpClient<EmbeddingClient>(client =>
{
    client.BaseAddress = new Uri(embeddingsBase);
});

builder.Services.AddScoped<VectorSearch>();
builder.Services.AddScoped<IngestService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "api" }));

// SignalR hub endpoint
app.MapHub<NeuroGraphHub>("/hub/graph");

// Ingest
app.MapPost("/api/ingest", async (IngestRequest req, IngestService ingest, IHubContext<NeuroGraphHub> hub) =>
{
    var doc = await ingest.IngestAsync(req);
    await hub.Clients.All.SendAsync("ingested", new { id = doc.Id, source = doc.Source });
    return Results.Ok(new { id = doc.Id });
});

// Query
app.MapPost("/api/query", async (QueryRequest req, VectorSearch vs) =>
{
    var results = await vs.SearchAsync(req.Query, req.TopK ?? 5);
    return Results.Ok(new { hits = results });
});

app.Run();

// =======================
// EF Core DbContext
// =======================
public class AppDb : DbContext   // <-- public fixes CS0051 later
{
    public DbSet<Document> Documents => Set<Document>();
    public AppDb(DbContextOptions<AppDb> opts) : base(opts) { }

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.HasPostgresExtension("vector");
        mb.Entity<SearchRow>(e =>
{
    e.HasNoKey();                  // keyless
    e.ToView(null);                // it's a raw SQL projection, no backing view
    e.Property(p => p.Id).HasColumnName("id");
    e.Property(p => p.Source).HasColumnName("source");
    e.Property(p => p.Content).HasColumnName("content");
    e.Property(p => p.Score).HasColumnName("score");
});
    }
}
