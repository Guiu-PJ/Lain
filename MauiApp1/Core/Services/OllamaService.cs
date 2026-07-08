// Core/Services/OllamaService.cs
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Text.Json;

public class OllamaService
{
    private readonly HttpClient _http;
    private const string BaseUrl = "http://localhost:11434";

    public OllamaService(HttpClient http)
    {
        _http = http;
    }

    // Respuesta simple (sin streaming)
    public async Task<string> ChatAsync(List<(string Role, string Content)> history, string systemPrompt = "", string model = "llama3:8b")
    {
        var messages = new List<object>
    {
        new { role = "system", content = systemPrompt }
    };

        // Añade el historial reciente (últimos 10 mensajes para no llenar el contexto)
        foreach (var (role, content) in history.TakeLast(10))
            messages.Add(new { role, content });

        var body = new { model, stream = false, messages };

        var response = await _http.PostAsJsonAsync($"{BaseUrl}/api/chat", body);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<OllamaChatResponse>();
        return result?.Message?.Content ?? "(sin respuesta)";
    }

    // Streaming — para que el texto aparezca palabra a palabra
    public async IAsyncEnumerable<string> ChatStreamAsync(
        string prompt,
        string systemPrompt = "",
        string model = "mistral:latest")
    {
        var body = new
        {
            model,
            stream = true,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user",   content = prompt }
            }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/api/chat")
        {
            Content = JsonContent.Create(body)
        };

        using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        using var stream = await response.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrEmpty(line)) continue;

            var chunk = JsonSerializer.Deserialize<OllamaChatResponse>(line);
            if (chunk?.Message?.Content is { } text)
                yield return text;
        }
    }

    public async Task<string?> DetectMemoryAsync(string userMessage, string model = "llama3:8b")
    {
        var systemPrompt = """
        Tu única tarea es analizar el mensaje del usuario y decidir si contiene
        información personal relevante que merezca recordarse a largo plazo
        (nombre, gustos, trabajo, preferencias, datos personales importantes).

        Si encuentras algo así, responde ÚNICAMENTE con una frase corta en tercera
        persona que resuma el dato, por ejemplo: "El usuario se llama Guiu."

        Si NO hay nada relevante que recordar, responde ÚNICAMENTE con: NADA

        No expliques tu razonamiento. No añadas texto extra. Solo la frase o NADA.
        """;

        var body = new
        {
            model,
            stream = false,
            messages = new[]
            {
            new { role = "system", content = systemPrompt },
            new { role = "user", content = userMessage }
        }
        };

        var response = await _http.PostAsJsonAsync($"{BaseUrl}/api/chat", body);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<OllamaChatResponse>();
        var texto = result?.Message?.Content?.Trim();

        return (string.IsNullOrEmpty(texto) || texto.Equals("NADA", StringComparison.OrdinalIgnoreCase))
            ? null
            : texto;
    }
}

// Modelos de respuesta
public record OllamaChatResponse(
    [property: JsonPropertyName("message")] OllamaMessage? Message,
    [property: JsonPropertyName("done")] bool Done
);

public record OllamaMessage(
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("content")] string Content
);