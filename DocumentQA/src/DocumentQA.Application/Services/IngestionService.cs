using DocumentQA.Application.Configuration;
using DocumentQA.Application.Interfaces;
using DocumentQA.Domain.Entities;
using Microsoft.Extensions.Options;

namespace DocumentQA.Application.Services;

public interface IIngestionService
{
    Task<Guid> QueueDocumentAsync(string userId, string filename, string contentType, Stream fileStream, CancellationToken ct = default);
    Task ProcessDocumentAsync(IngestionJob job, CancellationToken ct = default);
}

public record IngestionJob(Guid DocumentId, string UserId, string FilePath, string ContentType);

public class IngestionService : IIngestionService
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IVectorRepository _vectorRepository;
    private readonly ITextExtractor _textExtractor;
    private readonly IChunkingService _chunkingService;
    private readonly IEmbeddingService _embeddingService;
    private readonly IngestionSettings _settings;
    private readonly IngestionQueue _queue;

    public IngestionService(
        IDocumentRepository documentRepository,
        IVectorRepository vectorRepository,
        ITextExtractor textExtractor,
        IChunkingService chunkingService,
        IEmbeddingService embeddingService,
        IOptions<IngestionSettings> settings,
        IngestionQueue queue)
    {
        _documentRepository = documentRepository;
        _vectorRepository = vectorRepository;
        _textExtractor = textExtractor;
        _chunkingService = chunkingService;
        _embeddingService = embeddingService;
        _settings = settings.Value;
        _queue = queue;
    }

    public async Task<Guid> QueueDocumentAsync(
        string userId,
        string filename,
        string contentType,
        Stream fileStream,
        CancellationToken ct = default)
    {
        // Validate
        if (!_settings.AllowedContentTypes.Contains(contentType))
            throw new InvalidOperationException($"Content type '{contentType}' is not supported.");

        if (fileStream.Length > _settings.MaxFileSizeMb * 1024 * 1024)
            throw new InvalidOperationException($"File size exceeds {_settings.MaxFileSizeMb}MB limit.");

        // Save file temporarily
        var tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}_{filename}");
        await using (var fileStreamOut = File.Create(tempPath))
        {
            await fileStream.CopyToAsync(fileStreamOut, ct);
        }

        // Create document record
        var document = new Document
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Filename = filename,
            ContentType = contentType,
            FileSizeBytes = fileStream.Length,
            Status = DocumentStatus.Pending
        };

        await _documentRepository.CreateAsync(document, ct);

        // Queue for processing
        var job = new IngestionJob(document.Id, userId, tempPath, contentType);
        await _queue.EnqueueAsync(job, ct);

        return document.Id;
    }

    public async Task ProcessDocumentAsync(IngestionJob job, CancellationToken ct = default)
    {
        try
        {
            // Update status
            await _documentRepository.UpdateStatusAsync(job.DocumentId, DocumentStatus.Processing, ct: ct);

            // Extract text
            await using var fileStream = File.OpenRead(job.FilePath);
            var text = await _textExtractor.ExtractAsync(fileStream, job.ContentType, ct);

            if (string.IsNullOrWhiteSpace(text))
                throw new InvalidOperationException("No text content could be extracted from the document.");

            // Chunk
            var chunkResults = _chunkingService.Chunk(text);

            if (chunkResults.Count == 0)
                throw new InvalidOperationException("Document produced no chunks.");

            // Embed all chunks in batch
            var embeddings = await _embeddingService.EmbedBatchAsync(
                chunkResults.Select(c => c.Content),
                ct);

            // Create chunk entities
            var chunks = chunkResults.Select((c, i) => new Chunk
            {
                Id = Guid.NewGuid(),
                DocumentId = job.DocumentId,
                Content = c.Content,
                Embedding = embeddings[i],
                ChunkIndex = c.Index,
                TokenCount = c.TokenEstimate
            }).ToList();

            // Store chunks
            await _vectorRepository.InsertChunksBatchAsync(chunks, ct);

            // Update document status
            await _documentRepository.UpdateStatusAsync(
                job.DocumentId,
                DocumentStatus.Completed,
                chunkCount: chunks.Count,
                ct: ct);
        }
        catch (Exception ex)
        {
            await _documentRepository.UpdateStatusAsync(
                job.DocumentId,
                DocumentStatus.Failed,
                errorMessage: ex.Message,
                ct: ct);
            throw;
        }
        finally
        {
            // Cleanup temp file
            if (File.Exists(job.FilePath))
                File.Delete(job.FilePath);
        }
    }
}
