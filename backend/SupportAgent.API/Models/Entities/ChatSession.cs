using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SupportAgent.API.Models.Entities;

public class ChatSession
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    [BsonElement("sessionId")]
    public string SessionId { get; set; } = Guid.NewGuid().ToString();

    [BsonElement("messages")]
    public List<SessionMessage> Messages { get; set; } = [];

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class SessionMessage
{
    [BsonElement("role")]
    public string Role { get; set; } = null!;   // "user" | "assistant"

    [BsonElement("content")]
    public string Content { get; set; } = null!;

    /// <summary>Article ObjectId strings that informed this message.</summary>
    [BsonElement("sourceIds")]
    public List<string> SourceIds { get; set; } = [];

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
