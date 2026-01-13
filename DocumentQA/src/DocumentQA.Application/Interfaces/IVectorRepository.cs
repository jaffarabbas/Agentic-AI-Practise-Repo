using DocumentQA.Domain.Entities;

namespace DocumentQA.Application.Interfaces;

public interface IVectorRepository
{
    Task InsertChunkAsync(Chunk chunk, CancellationToken ct = default);
    Task InsertChunksBatchAsync(IEnumerable<Chunk> chunks, CancellationToken ct = default);
    Task<List<SearchResult>> SearchAsync(float[] queryEmbedding, int topK, float minSimilarity, string? userId = null, Guid? documentId = null, CancellationToken ct = default);
    Task DeleteByDocumentIdAsync(Guid documentId, CancellationToken ct = default);
}
