namespace DocumentQA.Application.Interfaces;

public interface IChatService
{
    Task<ChatResponse> CompleteAsync(string userMessage, string? systemPrompt = null, CancellationToken ct = default);
    IAsyncEnumerable<string> StreamAsync(string userMessage, string? systemPrompt = null, CancellationToken ct = default);
}

public record ChatResponse(string Content, int InputTokens, int OutputTokens);
