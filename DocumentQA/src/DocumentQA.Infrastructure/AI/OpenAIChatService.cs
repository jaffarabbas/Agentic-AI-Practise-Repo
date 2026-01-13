using System.Runtime.CompilerServices;
using DocumentQA.Application.Configuration;
using DocumentQA.Application.Interfaces;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;

namespace DocumentQA.Infrastructure.AI;

public class OpenAIChatService : IChatService
{
    private readonly ChatClient _client;

    public OpenAIChatService(IOptions<OpenAISettings> settings)
    {
        var openAiClient = new OpenAIClient(settings.Value.ApiKey);
        _client = openAiClient.GetChatClient(settings.Value.ChatModel);
    }

    public async Task<ChatResponse> CompleteAsync(
        string userMessage,
        string? systemPrompt = null,
        CancellationToken ct = default)
    {
        var messages = new List<ChatMessage>();

        if (!string.IsNullOrEmpty(systemPrompt))
            messages.Add(new SystemChatMessage(systemPrompt));

        messages.Add(new UserChatMessage(userMessage));

        var response = await _client.CompleteChatAsync(messages, cancellationToken: ct);

        return new ChatResponse(
            response.Value.Content[0].Text,
            response.Value.Usage.InputTokenCount,
            response.Value.Usage.OutputTokenCount
        );
    }

    public async IAsyncEnumerable<string> StreamAsync(
        string userMessage,
        string? systemPrompt = null,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var messages = new List<ChatMessage>();

        if (!string.IsNullOrEmpty(systemPrompt))
            messages.Add(new SystemChatMessage(systemPrompt));

        messages.Add(new UserChatMessage(userMessage));

        var streamingUpdates = _client.CompleteChatStreamingAsync(messages, cancellationToken: ct);

        await foreach (var update in streamingUpdates.WithCancellation(ct))
        {
            foreach (var contentPart in update.ContentUpdate)
            {
                if (!string.IsNullOrEmpty(contentPart.Text))
                    yield return contentPart.Text;
            }
        }
    }
}
