namespace DocumentQA.Application.Interfaces;

public interface ITextExtractor
{
    Task<string> ExtractAsync(Stream fileStream, string contentType, CancellationToken ct = default);
    bool CanExtract(string contentType);
}
