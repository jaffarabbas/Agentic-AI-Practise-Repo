namespace DocumentQA.Domain.Entities;

public class Document
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Filename { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public long? FileSizeBytes { get; set; }
    public int ChunkCount { get; set; }
    public DocumentStatus Status { get; set; } = DocumentStatus.Pending;
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
}

public enum DocumentStatus
{
    Pending,
    Processing,
    Completed,
    Failed
}
