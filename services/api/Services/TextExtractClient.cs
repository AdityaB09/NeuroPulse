public class TextExtractClient
{
    private readonly HttpClient _http;
    public TextExtractClient(HttpClient http) { _http = http; _http.Timeout = TimeSpan.FromSeconds(30); }

    // Returns a list of extracted text parts (pages/sections)
    public async Task<List<string>> ExtractAsync(IFormFile file, CancellationToken ct = default)
    {
        using var form = new MultipartFormDataContent();
        var stream = file.OpenReadStream();
        form.Add(new StreamContent(stream), "file", file.FileName);

        using var res = await _http.PostAsync("/extract", form, ct);
        res.EnsureSuccessStatusCode();
        var payload = await res.Content.ReadFromJsonAsync<ExtractResponse>(cancellationToken: ct);
        return payload!.parts;
    }

    private record ExtractResponse(List<string> parts);
}
