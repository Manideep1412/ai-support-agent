using System.Runtime.CompilerServices;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Embeddings;

namespace SupportAgent.API.Services;

public class OpenAIService
{
    private readonly OpenAIClient _client;
    private readonly string _chatModel;
    private readonly string _embeddingModel;
    private readonly ILogger<OpenAIService> _logger;

    public OpenAIService(IConfiguration config, ILogger<OpenAIService> logger)
    {
        var apiKey = config["OpenAI:ApiKey"]
            ?? throw new InvalidOperationException("OpenAI:ApiKey is required. Set it in appsettings.json or as an environment variable.");

        _client        = new OpenAIClient(apiKey);
        _chatModel     = config["OpenAI:ChatModel"]      ?? "gpt-4o-mini";
        _embeddingModel = config["OpenAI:EmbeddingModel"] ?? "text-embedding-3-small";
        _logger        = logger;
    }

    /// <summary>Generates a 1536-dim embedding vector for the given text.</summary>
    public async Task<double[]> GenerateEmbeddingAsync(string text, CancellationToken ct = default)
    {
        var client = _client.GetEmbeddingClient(_embeddingModel);
        var result = await client.GenerateEmbeddingAsync(text, cancellationToken: ct);
        return result.Value.ToFloats().ToArray().Select(f => (double)f).ToArray();
    }

    /// <summary>Streams a chat completion, yielding text deltas as they arrive.</summary>
    public async IAsyncEnumerable<string> StreamChatAsync(
        string systemPrompt,
        IEnumerable<(string Role, string Content)> history,
        string userMessage,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var chatClient = _client.GetChatClient(_chatModel);

        var messages = new List<ChatMessage> { new SystemChatMessage(systemPrompt) };

        foreach (var (role, content) in history)
            messages.Add(role == "user"
                ? (ChatMessage)new UserChatMessage(content)
                : new AssistantChatMessage(content));

        messages.Add(new UserChatMessage(userMessage));

        _logger.LogInformation("Streaming chat — model: {Model}, messages: {Count}", _chatModel, messages.Count);

        await foreach (var update in chatClient.CompleteChatStreamingAsync(messages, cancellationToken: ct))
        {
            foreach (var part in update.ContentUpdate)
            {
                if (!string.IsNullOrEmpty(part.Text))
                    yield return part.Text;
            }
        }
    }
}
