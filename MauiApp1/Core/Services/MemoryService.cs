using Microsoft.EntityFrameworkCore;
using MiAplicacion.Data;
using MiAplicacion.Data.Models;

public class MemoryService
{
    private readonly AppDbContext _db;
    private readonly EmbeddingService _embeddings;

    // Memorias con importancia igual o mayor a este umbral se incluyen SIEMPRE
    // en el contexto, sin importar qué tan relevante sea semánticamente el mensaje.
    // Súbelo si quieres que solo entren cosas realmente críticas.
    private const float AlwaysIncludeImportanceThreshold = 4.5f;

    public MemoryService(AppDbContext db, EmbeddingService embeddings)
    {
        _db = db;
        _embeddings = embeddings;
        _db.Database.EnsureCreated();
    }

    public async Task<List<Memory>> GetAllAsync() =>
        await _db.Memories.OrderByDescending(m => m.Importance).ToListAsync();

    public async Task SaveAsync(string content, string? category = null, float importance = 1.0f)
    {
        byte[]? embeddingBytes = null;

        try
        {
            var embedding = await _embeddings.GetEmbeddingAsync(content);
            if (embedding.Length > 0)
                embeddingBytes = SerializeEmbedding(embedding);
        }
        catch
        {
            // Si Ollama no está disponible o falla, se guarda la memoria igual,
            // simplemente no participará en la búsqueda semántica hasta que se regenere.
        }

        _db.Memories.Add(new Memory
        {
            Content = content,
            Category = category,
            Importance = importance,
            Embedding = embeddingBytes
        });

        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var m = await _db.Memories.FindAsync(id);
        if (m is null) return;
        _db.Memories.Remove(m);
        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// RAG: devuelve las memorias más relevantes para el mensaje actual.
    /// Combina:
    ///  - Top-K por similitud semántica (embedding del mensaje vs. embedding de cada memoria)
    ///  - Memorias de importancia alta, siempre incluidas aunque no sean semánticamente cercanas
    /// Actualiza LastUsedAt de las memorias devueltas.
    /// </summary>
    public async Task<List<Memory>> SearchRelevantAsync(string userMessage, int topK = 5)
    {
        var all = await _db.Memories.ToListAsync();
        if (all.Count == 0) return new List<Memory>();

        var alwaysInclude = all
            .Where(m => m.Importance >= AlwaysIncludeImportanceThreshold)
            .ToList();

        List<Memory> semantic = new();

        if (!string.IsNullOrWhiteSpace(userMessage))
        {
            float[] queryEmbedding = Array.Empty<float>();
            try
            {
                queryEmbedding = await _embeddings.GetEmbeddingAsync(userMessage);
            }
            catch
            {
                // Sin conexión al modelo de embeddings: caemos al fallback de abajo.
            }

            if (queryEmbedding.Length > 0)
            {
                semantic = all
                    .Where(m => m.Embedding is not null)
                    .Select(m => (
                        Memory: m,
                        Score: CosineSimilarity(queryEmbedding, DeserializeEmbedding(m.Embedding!))))
                    .OrderByDescending(x => x.Score)
                    .Take(topK)
                    .Select(x => x.Memory)
                    .ToList();
            }
        }

        if (semantic.Count == 0)
        {
            // Fallback: sin embeddings disponibles, usamos las más importantes/recientes.
            semantic = all
                .OrderByDescending(m => m.Importance)
                .ThenByDescending(m => m.CreatedAt)
                .Take(topK)
                .ToList();
        }

        var combined = alwaysInclude
            .Concat(semantic)
            .GroupBy(m => m.Id)
            .Select(g => g.First())
            .OrderByDescending(m => m.Importance)
            .ToList();

        var now = DateTime.Now;
        foreach (var m in combined)
            m.LastUsedAt = now;

        if (combined.Count > 0)
            await _db.SaveChangesAsync();

        return combined;
    }

    private static float CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length == 0 || b.Length == 0 || a.Length != b.Length)
            return 0f;

        float dot = 0f, normA = 0f, normB = 0f;
        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }

        if (normA == 0 || normB == 0) return 0f;
        return dot / (MathF.Sqrt(normA) * MathF.Sqrt(normB));
    }

    private static byte[] SerializeEmbedding(float[] embedding)
    {
        var bytes = new byte[embedding.Length * sizeof(float)];
        Buffer.BlockCopy(embedding, 0, bytes, 0, bytes.Length);
        return bytes;
    }

    private static float[] DeserializeEmbedding(byte[] bytes)
    {
        var floats = new float[bytes.Length / sizeof(float)];
        Buffer.BlockCopy(bytes, 0, floats, 0, bytes.Length);
        return floats;
    }

}