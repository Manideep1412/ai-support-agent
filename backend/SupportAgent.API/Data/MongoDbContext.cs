using MongoDB.Driver;
using SupportAgent.API.Models.Entities;

namespace SupportAgent.API.Data;

public class MongoDbContext
{
    private readonly IMongoDatabase _db;

    public MongoDbContext(IMongoClient client, IConfiguration config)
    {
        _db = client.GetDatabase(config["MongoDB:Database"] ?? "support_agent");
    }

    public IMongoCollection<KnowledgeArticle> Articles =>
        _db.GetCollection<KnowledgeArticle>("knowledge_articles");

    public IMongoCollection<ChatSession> Sessions =>
        _db.GetCollection<ChatSession>("chat_sessions");
}
