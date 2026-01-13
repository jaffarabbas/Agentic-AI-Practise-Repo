namespace DocumentQA.Application.Interfaces;

public interface IEmbeddingService
{
    Task<float[]> EmbedAsync(string text, CancellationToken ct = default);
    Task<float[][]> EmbedBatchAsync(IEnumerable<string> texts, CancellationToken ct = default);
}
