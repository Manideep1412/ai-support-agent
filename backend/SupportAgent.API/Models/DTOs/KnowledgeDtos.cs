using System.ComponentModel.DataAnnotations;

namespace SupportAgent.API.Models.DTOs;

public record CreateArticleRequest(
    [Required, MaxLength(200)]   string Title,
    [Required, MaxLength(10000)] string Content,
    string Category = "General"
);

public record UpdateArticleRequest(
    [Required, MaxLength(200)]   string Title,
    [Required, MaxLength(10000)] string Content,
    string Category = "General"
);

public record ArticleDto(
    string   Id,
    string   Title,
    string   Content,
    string   Category,
    bool     HasEmbedding,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record EmbedAllResult(int Embedded, int Failed);
