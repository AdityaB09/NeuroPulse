namespace NeuroPulse.Api.Services;

using System.Net.Http.Json;

public class EmbeddingClient
{
    private readonly HttpClient _http;
    public EmbeddingClient(HttpClient http) => _http = http;

    public async Task<float[]> EmbedAsync(string text)
    {
        var res = await _http.PostAsJsonAsync("/embed", new { text });
        res.EnsureSuccessStatusCode();
        var payload = await res.Content.ReadFromJsonAsync<EmbedResponse>();
        return payload!.vector;
    }

    private record EmbedResponse(float[] vector);
}
