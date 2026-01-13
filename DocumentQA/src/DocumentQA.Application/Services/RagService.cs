using DocumentQA.Application.Configuration;
using DocumentQA.Application.DTOs;
using DocumentQA.Application.Interfaces;
using Microsoft.Extensions.Options;

namespace DocumentQA.Application.Services;

public interface IRagService
{
    Task<AskResponse> AskAsync(string userId, AskRequest request, CancellationToken ct = default);
    IAsyncEnumerable<string> AskStreamAsync(string userId, AskRequest request, CancellationToken ct = default);
}

public class RagService : IRagService
{
    private readonly IEmbeddingService _embeddingService;
    private readonly IChatService _chatService;
    private readonly IVectorRepository _vectorRepository;
    private readonly RagSettings _settings;

    private const string SystemPromptTemplate = """
        You are a helpful assistant that answers questions based ONLY on the provided context.

        Rules:
        - Answer based ONLY on the context provided below
        - If the context doesn't contain enough information to answer, say "I don't have enough information to answer that question based on the provided documents."
        - Do not make up information or use knowledge outside the context
        - Be concise and direct
        - If quoting from the context, be accurate

        Context:
        {context}
        """;

    public RagService(
        IEmbeddingService embeddingService,
        IChatService chatService,
        IVectorRepository vectorRepository,
        IOptions<RagSettings> settings)
    {
        _embeddingService = embeddingService;
        _chatService = chatService;
        _vectorRepository = vectorRepository;
        _settings = settings.Value;
    }

    public async Task<AskResponse> AskAsync(string userId, AskRequest request, CancellationToken ct = default)
    {
        // 1. Embed the question
        var queryEmbedding = await _embeddingService.EmbedAsync(request.Question, ct);

        // 2. Search for relevant chunks
        var topK = request.TopK ?? _settings.TopK;
        var searchResults = await _vectorRepository.SearchAsync(
            queryEmbedding,
            topK,
            _settings.MinSimilarity,
            userId,
            request.DocumentId,
            ct);

        // 3. Build context from search results
        var context = BuildContext(searchResults);

        // 4. Generate answer
        var systemPrompt = SystemPromptTemplate.Replace("{context}", context);
        var response = await _chatService.CompleteAsync(request.Question, systemPrompt, ct);

        // 5. Build response
        var sources = searchResults.Select(r => new SourceChunkDto(
            r.ChunkId,
            r.DocumentId,
            TruncateContent(r.Content, 200),
            r.Similarity,
            r.Filename
        )).ToList();

        var cost = CalculateCost(response.InputTokens, response.OutputTokens);

        return new AskResponse(
            response.Content,
            sources,
            new UsageInfoDto(response.InputTokens, response.OutputTokens, cost)
        );
    }

    public async IAsyncEnumerable<string> AskStreamAsync(
        string userId,
        AskRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        // 1. Embed the question
        var queryEmbedding = await _embeddingService.EmbedAsync(request.Question, ct);

        // 2. Search for relevant chunks
        var topK = request.TopK ?? _settings.TopK;
        var searchResults = await _vectorRepository.SearchAsync(
            queryEmbedding,
            topK,
            _settings.MinSimilarity,
            userId,
            request.DocumentId,
            ct);

        // 3. Build context
        var context = BuildContext(searchResults);
        var systemPrompt = SystemPromptTemplate.Replace("{context}", context);

        // 4. Stream response
        await foreach (var chunk in _chatService.StreamAsync(request.Question, systemPrompt, ct))
        {
            yield return chunk;
        }
    }

    private static string BuildContext(List<Domain.Entities.SearchResult> results)
    {
        if (results.Count == 0)
            return "No relevant context found.";

        return string.Join("\n\n---\n\n", results.Select((r, i) =>
            $"[Source {i + 1}] (Relevance: {r.Similarity:P0})\n{r.Content}"));
    }

    private static string TruncateContent(string content, int maxLength)
    {
        if (content.Length <= maxLength)
            return content;
        return content[..(maxLength - 3)] + "...";
    }

    private static decimal CalculateCost(int inputTokens, int outputTokens)
    {
        // GPT-4o-mini pricing (as of 2024)
        const decimal inputCostPer1K = 0.00015m;
        const decimal outputCostPer1K = 0.0006m;

        return (inputTokens * inputCostPer1K / 1000) + (outputTokens * outputCostPer1K / 1000);
    }
}
