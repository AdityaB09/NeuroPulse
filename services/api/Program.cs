using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Pgvector.EntityFrameworkCore;
using StackExchange.Redis;
using NeuroPulse.Api.Data;
using NeuroPulse.Api.Services;
using NeuroPulse.Api.Models;

namespace NeuroPulse.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Config
        var dbCxn = builder.Configuration.GetConnectionString("Db")!;
        var redisUrl = builder.Configuration.GetSection("Redis").GetValue<string>("Url") ?? "redis:6379";
        var embeddingsBase = builder.Configuration.GetSection("Embeddings").GetValue<string>("BaseUrl") ?? "http://embeddings:8000";

        // Services
        builder.Services.AddDbContext<AppDb>(opt =>
            opt.UseNpgsql(dbCxn, npg => npg.UseVector()));

        builder.Services.AddSignalR();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisUrl));

        builder.Services.AddHttpClient<EmbeddingClient>(c => c.BaseAddress = new Uri(embeddingsBase));
        builder.Services.AddScoped<VectorSearch>();
        builder.Services.AddScoped<IngestService>();

        var app = builder.Build();

        app.UseSwagger();
        app.UseSwaggerUI();

        app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "api" }));

        // SignalR hub for live events
        app.MapHub<NeuroGraphHub>("/hub/graph");

        // Ingest
        app.MapPost("/api/ingest", async (IngestRequest req, IngestService ingest, IHubContext<NeuroGraphHub> hub) =>
        {
            var doc = await ingest.IngestAsync(req);
            await hub.Clients.All.SendAsync("ingested", new { id = doc.Id, source = doc.Source });
            return Results.Ok(new { id = doc.Id });
        });

        // Vector query
        app.MapPost("/api/query", async (QueryRequest req, VectorSearch vs) =>
        {
            var results = await vs.SearchAsync(req.Query, Math.Clamp(req.TopK ?? 5, 1, 50));
            return Results.Ok(new { hits = results });
        });

        app.Run();
    }
}
