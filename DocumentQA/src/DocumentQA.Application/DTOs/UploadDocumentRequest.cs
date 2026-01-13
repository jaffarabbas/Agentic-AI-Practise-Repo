namespace DocumentQA.Application.DTOs;

public record UploadDocumentResponse(
    Guid DocumentId,
    string Status,
    string Message
);
