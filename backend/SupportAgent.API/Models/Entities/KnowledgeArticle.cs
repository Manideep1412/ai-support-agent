using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SupportAgent.API.Models.Entities;

public class KnowledgeArticle
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    [BsonElement("title")]
    public string Title { get; set; } = null!;

    [BsonElement("content")]
    public string Content { get; set; } = null!;

    [BsonElement("category")]
    public string Category { get; set; } = "General";

    /// <summary>
    /// 1 536-dim vector from text-embedding-3-small.
    /// Populated by POST /api/knowledge/{id}/embed or POST /api/knowledge/embed-all.
    /// </summary>
    [BsonElement("embedding")]
    public List<double> Embedding { get; set; } = [];

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
