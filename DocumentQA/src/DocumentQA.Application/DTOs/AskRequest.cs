namespace DocumentQA.Application.DTOs;

public record AskRequest(
    string Question,
    Guid? DocumentId = null,
    int? TopK = null
);

public record AskResponse(
    string Answer,
    List<SourceChunkDto> Sources,
    UsageInfoDto Usage
);

public record SourceChunkDto(
    Guid ChunkId,
    Guid DocumentId,
    string ContentPreview,
    float Similarity,
    string? Filename
);

public record UsageInfoDto(
    int InputTokens,
    int OutputTokens,
    decimal EstimatedCost
);
