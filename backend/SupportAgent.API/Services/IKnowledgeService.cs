using SupportAgent.API.Models.DTOs;
using SupportAgent.API.Models.Entities;

namespace SupportAgent.API.Services;

public interface IKnowledgeService
{
    Task<List<ArticleDto>> GetAllAsync(CancellationToken ct = default);
    Task<ArticleDto?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<ArticleDto> CreateAsync(CreateArticleRequest req, CancellationToken ct = default);
    Task<ArticleDto?> UpdateAsync(string id, UpdateArticleRequest req, CancellationToken ct = default);
    Task<bool> DeleteAsync(string id, CancellationToken ct = default);
    Task<ArticleDto?> EmbedArticleAsync(string id, CancellationToken ct = default);
    Task<EmbedAllResult> EmbedAllAsync(CancellationToken ct = default);
    Task<List<KnowledgeArticle>> SearchSimilarAsync(double[] queryEmbedding, int limit = 3, CancellationToken ct = default);
}
