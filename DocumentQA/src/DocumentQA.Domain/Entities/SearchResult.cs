namespace DocumentQA.Domain.Entities;

public class SearchResult
{
    public Guid ChunkId { get; set; }
    public Guid DocumentId { get; set; }
    public string Content { get; set; } = string.Empty;
    public float Similarity { get; set; }
    public string? Filename { get; set; }
}
