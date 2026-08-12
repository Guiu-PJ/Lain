
public class Memory
{
    public int Id { get; set; }
    public string Content { get; set; } = "";        
    public string? Category { get; set; }           
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? LastUsedAt { get; set; }
    public float Importance { get; set; } = 1.0f;

    public byte[]? Embedding { get; set; }
}