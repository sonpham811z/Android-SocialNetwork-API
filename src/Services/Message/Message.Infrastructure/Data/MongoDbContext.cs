using Message.Domain.Entities;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace Message.Infrastructure.Data;

public class MongoDbContext
{
    static MongoDbContext()
    {
        BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));
    }

    private readonly IMongoDatabase _database;

    public MongoDbContext(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("MongoDB")
            ?? throw new InvalidOperationException("MongoDB connection string 'MongoDB' is not configured.");

        var databaseName = configuration["MongoDB:DatabaseName"] ?? "MessageDb";

        var client = new MongoClient(connectionString);
        _database  = client.GetDatabase(databaseName);
    }

    public IMongoCollection<Conversation> Conversations
        => _database.GetCollection<Conversation>("conversations");

    public IMongoCollection<ChatMessage> Messages
        => _database.GetCollection<ChatMessage>("messages");
}
