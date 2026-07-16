using System.Runtime.CompilerServices;
using System.Text;
using MongoDB.Driver;
using SupportAgent.API.Data;
using SupportAgent.API.Models.DTOs;
using SupportAgent.API.Models.Entities;

namespace SupportAgent.API.Services;

public class ChatService
{
    private readonly MongoDbContext _db;
    private readonly OpenAIService _ai;
    private readonly KnowledgeService _knowledge;
    private readonly ILogger<ChatService> _logger;

    private const string SystemPromptTemplate = """
        You are a helpful, friendly customer support assistant.

        First, check the KNOWLEDGE BASE CONTEXT below. If the answer is there, use it and be specific.
        If the knowledge base doesn't cover the question, use your general knowledge to give a helpful answer —
        but add a brief note like "(Based on general knowledge — not specific to our platform)".
        Never make up company-specific details (pricing, policies, account data) that aren't in the knowledge base.
        Be concise and professional.

        ── KNOWLEDGE BASE CONTEXT ──────────────────────────────────
        {0}
        ────────────────────────────────────────────────────────────
        """;

    public ChatService(
        MongoDbContext db,
        OpenAIService ai,
        KnowledgeService knowledge,
        ILogger<ChatService> logger)
    {
        _db       = db;
        _ai       = ai;
        _knowledge = knowledge;
        _logger   = logger;
    }

    /// <summary>
    /// Runs the RAG pipeline: embeds the question, retrieves relevant articles,
    /// builds the system prompt, then returns a streaming async enumerable of
    /// text chunks together with source citations and session metadata.
    ///
    /// The caller (controller) is responsible for forwarding chunks to the client.
    /// Persistence happens automatically once the stream is exhausted.
    /// </summary>
    public async Task<(IAsyncEnumerable<string> Stream, List<SourceReference> Sources, string SessionId)>
        AskAsync(AskRequest req, CancellationToken ct = default)
    {
        // 1. Get / create session
        var sessionId = req.SessionId ?? Guid.NewGuid().ToString();
        var session   = await GetOrCreateSessionAsync(sessionId, ct);

        // 2. Embed the question
        var questionEmbedding = await _ai.GenerateEmbeddingAsync(req.Question, ct);

        // 3. Retrieve top-k relevant articles (vector search → text fallback)
        var articles = await _knowledge.SearchSimilarAsync(questionEmbedding, limit: 3, ct: ct);
        var sources  = articles.Select(a => new SourceReference(a.Id, a.Title, a.Category, a.Content)).ToList();

        _logger.LogInformation("RAG: found {Count} source articles for query '{Question}'",
            articles.Count, req.Question);

        // 4. Build system prompt with injected context
        var context      = BuildContext(articles);
        var systemPrompt = string.Format(SystemPromptTemplate, context);

        // 5. Pass recent conversation history for multi-turn support (last 6 messages)
        var history = session.Messages
            .TakeLast(6)
            .Select(m => (m.Role, m.Content));

        // 6. Return the stream — persistence is wired inside StreamAndSaveAsync
        var stream = StreamAndSaveAsync(
            session, req.Question, systemPrompt, history, sources, ct);

        return (stream, sources, sessionId);
    }

    public async Task<SessionDto?> GetSessionAsync(string sessionId, CancellationToken ct = default)
    {
        var session = await _db.Sessions
            .Find(s => s.SessionId == sessionId)
            .FirstOrDefaultAsync(ct);

        if (session is null) return null;

        // Resolve article titles for source references
        var allIds = session.Messages
            .SelectMany(m => m.SourceIds)
            .Distinct()
            .ToList();

        var articleMap = allIds.Count > 0
            ? (await _db.Articles
                .Find(Builders<KnowledgeArticle>.Filter.In(a => a.Id, allIds))
                .ToListAsync(ct))
                .ToDictionary(a => a.Id)
            : new Dictionary<string, KnowledgeArticle>();

        var messages = session.Messages.Select(m => new MessageDto(
            m.Role,
            m.Content,
            m.SourceIds
                .Where(id => articleMap.ContainsKey(id))
                .Select(id => new SourceReference(id, articleMap[id].Title, articleMap[id].Category, articleMap[id].Content))
                .ToList(),
            m.CreatedAt
        )).ToList();

        return new SessionDto(session.SessionId, messages, session.CreatedAt);
    }

    public async Task<bool> DeleteSessionAsync(string sessionId, CancellationToken ct = default)
    {
        var result = await _db.Sessions.DeleteOneAsync(s => s.SessionId == sessionId, ct);
        return result.DeletedCount > 0;
    }

    public async Task<List<SessionSummaryDto>> ListSessionsAsync(int limit = 30, CancellationToken ct = default)
    {
        var sessions = await _db.Sessions
            .Find(FilterDefinition<ChatSession>.Empty)
            .SortByDescending(s => s.CreatedAt)
            .Limit(limit)
            .ToListAsync(ct);

        return sessions.Select(s =>
        {
            var first   = s.Messages.FirstOrDefault(m => m.Role == "user")?.Content ?? "New conversation";
            var preview = first.Length > 70 ? first[..70] + "…" : first;
            return new SessionSummaryDto(s.SessionId, preview, s.CreatedAt);
        }).ToList();
    }

    // ── Internals ─────────────────────────────────────────────────────────────

    private async IAsyncEnumerable<string> StreamAndSaveAsync(
        ChatSession session,
        string question,
        string systemPrompt,
        IEnumerable<(string Role, string Content)> history,
        List<SourceReference> sources,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var sb        = new StringBuilder();
        var sourceIds = sources.Select(s => s.Id).ToList();

        await foreach (var chunk in _ai.StreamChatAsync(systemPrompt, history, question, ct))
        {
            sb.Append(chunk);
            yield return chunk;
        }

        // Persist the exchange once streaming is complete
        session.Messages.Add(new SessionMessage { Role = "user",      Content = question,      SourceIds = sourceIds });
        session.Messages.Add(new SessionMessage { Role = "assistant", Content = sb.ToString(), SourceIds = sourceIds });

        await _db.Sessions.ReplaceOneAsync(
            s => s.SessionId == session.SessionId,
            session,
            new ReplaceOptions { IsUpsert = true },
            ct);
    }

    private async Task<ChatSession> GetOrCreateSessionAsync(string sessionId, CancellationToken ct)
    {
        var existing = await _db.Sessions.Find(s => s.SessionId == sessionId).FirstOrDefaultAsync(ct);
        if (existing is not null) return existing;

        var session = new ChatSession { SessionId = sessionId };
        await _db.Sessions.InsertOneAsync(session, cancellationToken: ct);
        return session;
    }

    private static string BuildContext(List<KnowledgeArticle> articles) =>
        articles.Count == 0
            ? "(No relevant articles found.)"
            : string.Join("\n\n---\n\n", articles.Select(a =>
                $"[{a.Category}] {a.Title}\n{a.Content}"));
}
