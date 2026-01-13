using DocumentQA.Domain.Entities;

namespace DocumentQA.Application.Interfaces;

public interface IDocumentRepository
{
    Task<Guid> CreateAsync(Document document, CancellationToken ct = default);
    Task<Document?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<Document>> GetByUserIdAsync(string userId, CancellationToken ct = default);
    Task UpdateStatusAsync(Guid id, DocumentStatus status, int? chunkCount = null, string? errorMessage = null, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
