namespace DocumentQA.Application.DTOs;

public record DocumentDto(
    Guid Id,
    string Filename,
    string? ContentType,
    long? FileSizeBytes,
    int ChunkCount,
    string Status,
    string? ErrorMessage,
    DateTime CreatedAt,
    DateTime? ProcessedAt
);
