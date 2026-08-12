public class ContextBuilder
{
    private readonly TaskService _tasks;
    private readonly MemoryService _memory;

    public ContextBuilder(TaskService tasks, MemoryService memory)
    {
        _tasks = tasks;
        _memory = memory;
    }

    public async Task<string> BuildAsync(ContextOptions options)
    {
        var sections = new List<string>();

        sections.Add($"""
            Eres Lain, un asistente personal con personalidad propia inspirada en Lain Iwakura.
            Eres tranquila, inteligente, algo enigmática, pero genuinamente útil y cercana.
            Hablas en español, de forma concisa. Nunca rompes el personaje.
            Hoy es {DateTime.Now:dddd, d MMMM yyyy}, son las {DateTime.Now:HH:mm}.
            """);

        if (options.IncludeMemory)
        {
            // RAG: en vez de mandar TODAS las memorias, mandamos solo las
            // relevantes al mensaje actual (+ las de importancia alta siempre).
            var memories = await _memory.SearchRelevantAsync(options.UserMessage, topK: 5);
            if (memories.Any())
            {
                var texto = string.Join("\n", memories.Select(m => $"- {m.Content}"));
                sections.Add($"## Lo que sé del usuario (relevante a este mensaje)\n{texto}");
            }
        }

        if (options.IncludeTasks)
        {
            var tareas = await _tasks.GetAllAsync();
            var pendientes = tareas.Where(t => !t.IsCompleted).ToList();
            var texto = pendientes.Any()
                ? string.Join("\n", pendientes.Select(t =>
                    $"- [{t.Priority}] {t.Title}" +
                    (t.DueDate.HasValue ? $" → {t.DueDate.Value:dd/MM/yyyy}" : "")))
                : "Sin tareas pendientes.";
            sections.Add($"## Tareas pendientes\n{texto}");
        }

        return string.Join("\n\n", sections);
    }
}

public class ContextOptions
{
    public bool IncludeTasks { get; set; } = true;
    public bool IncludeMemory { get; set; } = true;
    public string UserMessage { get; set; } = "";
}