using MongoDB.Bson;
using MongoDB.Driver;
using SupportAgent.API.Data;
using SupportAgent.API.Models.DTOs;
using SupportAgent.API.Models.Entities;

namespace SupportAgent.API.Services;

public class KnowledgeService
{
    private readonly MongoDbContext _db;
    private readonly OpenAIService _ai;
    private readonly ILogger<KnowledgeService> _logger;

    public KnowledgeService(MongoDbContext db, OpenAIService ai, ILogger<KnowledgeService> logger)
    {
        _db     = db;
        _ai     = ai;
        _logger = logger;
    }

    // ── CRUD ─────────────────────────────────────────────────────────────────

    public async Task<List<ArticleDto>> GetAllAsync(CancellationToken ct = default)
    {
        var articles = await _db.Articles
            .Find(FilterDefinition<KnowledgeArticle>.Empty)
            .SortByDescending(a => a.CreatedAt)
            .ToListAsync(ct);
        return articles.Select(ToDto).ToList();
    }

    public async Task<ArticleDto?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var a = await _db.Articles.Find(x => x.Id == id).FirstOrDefaultAsync(ct);
        return a is null ? null : ToDto(a);
    }

    public async Task<ArticleDto> CreateAsync(CreateArticleRequest req, CancellationToken ct = default)
    {
        var article = new KnowledgeArticle
        {
            Title     = req.Title.Trim(),
            Content   = req.Content.Trim(),
            Category  = req.Category.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        await _db.Articles.InsertOneAsync(article, cancellationToken: ct);
        return ToDto(article);
    }

    public async Task<ArticleDto?> UpdateAsync(string id, UpdateArticleRequest req, CancellationToken ct = default)
    {
        var update = Builders<KnowledgeArticle>.Update
            .Set(a => a.Title,     req.Title.Trim())
            .Set(a => a.Content,   req.Content.Trim())
            .Set(a => a.Category,  req.Category.Trim())
            .Set(a => a.Embedding, [])            // clear so it must be re-embedded
            .Set(a => a.UpdatedAt, DateTime.UtcNow);

        var result = await _db.Articles.FindOneAndUpdateAsync(
            a => a.Id == id, update,
            new FindOneAndUpdateOptions<KnowledgeArticle> { ReturnDocument = ReturnDocument.After },
            ct);

        return result is null ? null : ToDto(result);
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken ct = default)
    {
        var result = await _db.Articles.DeleteOneAsync(a => a.Id == id, ct);
        return result.DeletedCount > 0;
    }

    // ── Embeddings ────────────────────────────────────────────────────────────

    public async Task<ArticleDto?> EmbedArticleAsync(string id, CancellationToken ct = default)
    {
        var article = await _db.Articles.Find(a => a.Id == id).FirstOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException($"Article {id} not found.");

        var embedding = await _ai.GenerateEmbeddingAsync($"{article.Title}\n{article.Content}", ct);

        await _db.Articles.UpdateOneAsync(
            a => a.Id == id,
            Builders<KnowledgeArticle>.Update
                .Set(a => a.Embedding, embedding.ToList())
                .Set(a => a.UpdatedAt, DateTime.UtcNow),
            cancellationToken: ct);

        article.Embedding = embedding.ToList();
        _logger.LogInformation("Embedded article {Id}: {Title}", id, article.Title);
        return ToDto(article);
    }

    public async Task<EmbedAllResult> EmbedAllAsync(CancellationToken ct = default)
    {
        var articles = await _db.Articles
            .Find(FilterDefinition<KnowledgeArticle>.Empty)
            .ToListAsync(ct);

        var embedded = 0;
        var failed   = 0;

        foreach (var article in articles)
        {
            try
            {
                await EmbedArticleAsync(article.Id, ct);
                embedded++;
            }
            catch (Exception ex)
            {
                failed++;
                _logger.LogError(ex, "Failed to embed article {Id}: {Title}", article.Id, article.Title);
            }
        }

        return new EmbedAllResult(embedded, failed);
    }

    // ── Vector / text search ──────────────────────────────────────────────────

    /// <summary>
    /// Finds the top-k most semantically similar articles using MongoDB Atlas Vector Search.
    /// Falls back to returning recently-created articles if the vector index is not yet set up.
    /// </summary>
    public async Task<List<KnowledgeArticle>> SearchSimilarAsync(
        double[] queryEmbedding, int limit = 3, CancellationToken ct = default)
    {
        try
        {
            return await VectorSearchAsync(queryEmbedding, limit, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Vector search unavailable (index not configured?). " +
                "Run POST /api/knowledge/embed-all then create the 'vector_index' in MongoDB Atlas. " +
                "Falling back to most-recent articles.");

            // Fallback: return most-recently embedded articles.
            // LINQ .Count doesn't translate to MongoDB — use a proper driver filter.
            var embeddedFilter = Builders<KnowledgeArticle>.Filter.Not(
                Builders<KnowledgeArticle>.Filter.Size(a => a.Embedding, 0));

            return await _db.Articles
                .Find(embeddedFilter)
                .SortByDescending(a => a.UpdatedAt)
                .Limit(limit)
                .ToListAsync(ct);
        }
    }

    private async Task<List<KnowledgeArticle>> VectorSearchAsync(
        double[] queryEmbedding, int limit, CancellationToken ct)
    {
        var pipeline = new[]
        {
            new BsonDocument("$vectorSearch", new BsonDocument
            {
                { "index",       "vector_index" },
                { "path",        "embedding"    },
                { "queryVector", new BsonArray(queryEmbedding.Select(d => (BsonValue)d)) },
                { "numCandidates", Math.Max(limit * 10, 20) },
                { "limit",       limit }
            })
        };

        return await _db.Articles
            .Aggregate<KnowledgeArticle>(pipeline, cancellationToken: ct)
            .ToListAsync(ct);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static ArticleDto ToDto(KnowledgeArticle a) =>
        new(a.Id, a.Title, a.Content, a.Category, a.Embedding.Count > 0, a.CreatedAt, a.UpdatedAt);
}
