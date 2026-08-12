using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

public class EmbeddingService
{
    private readonly HttpClient _http;

    private const string Model = "nomic-embed-text";

    public EmbeddingService(HttpClient http)
    {
        _http = http;

        if (_http.BaseAddress is null)
            _http.BaseAddress = new Uri("http://localhost:11434");
    }

    public async Task<float[]> GetEmbeddingAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Array.Empty<float>();

        var response = await _http.PostAsJsonAsync("/api/embeddings", new
        {
            model = Model,
            prompt = text
        });

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<EmbeddingResponse>();
        return result?.Embedding ?? Array.Empty<float>();
    }

    private class EmbeddingResponse
    {
        public float[] Embedding { get; set; } = Array.Empty<float>();
    }
}
