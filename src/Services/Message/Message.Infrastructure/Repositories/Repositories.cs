using Message.Domain.Entities;
using Message.Domain.Interfaces;
using Message.Infrastructure.Data;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Message.Infrastructure.Repositories;

// ── Conversation Repository ───────────────────────────────────────────────────

public class ConversationRepository : IConversationRepository
{
    private readonly IMongoCollection<Conversation> _col;

    public ConversationRepository(MongoDbContext ctx) => _col = ctx.Conversations;

    public async Task<Conversation?> GetByIdAsync(string conversationId)
    {
        var filter = Builders<Conversation>.Filter.Eq(c => c.Id, conversationId);
        return await _col.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<Conversation?> GetOneToOneAsync(Guid user1, Guid user2)
    {
        // Both users must be in Members and size must be exactly 2
        var filter = Builders<Conversation>.Filter.And(
            Builders<Conversation>.Filter.Eq(c => c.Type, Domain.Enums.ConversationType.OneToOne),
            Builders<Conversation>.Filter.All(c => c.Members, new[] { user1, user2 }),
            Builders<Conversation>.Filter.Size(c => c.Members, 2));

        return await _col.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<Conversation>> GetByUserIdAsync(Guid userId)
    {
        var filter = Builders<Conversation>.Filter.AnyEq(c => c.Members, userId);
        return await _col
            .Find(filter)
            .SortByDescending(c => c.UpdatedAt)
            .ToListAsync();
    }

    public async Task<Conversation> CreateAsync(Conversation conversation)
    {
        await _col.InsertOneAsync(conversation);
        return conversation;
    }

    public async Task UpdateLastMessageAsync(string conversationId, LastMessageInfo lastMessage)
    {
        var filter = Builders<Conversation>.Filter.Eq(c => c.Id, conversationId);
        var update = Builders<Conversation>.Update
            .Set(c => c.LastMessage, lastMessage)
            .Set(c => c.UpdatedAt, DateTime.UtcNow);

        await _col.UpdateOneAsync(filter, update);
    }

    public async Task AddMemberAsync(string conversationId, Guid userId)
    {
        var filter = Builders<Conversation>.Filter.Eq(c => c.Id, conversationId);
        var update = Builders<Conversation>.Update
            .AddToSet(c => c.Members, userId)
            .Set(c => c.UpdatedAt, DateTime.UtcNow);

        await _col.UpdateOneAsync(filter, update);
    }
}

// ── Message Repository ────────────────────────────────────────────────────────

public class MessageRepository : IMessageRepository
{
    private readonly IMongoCollection<ChatMessage> _col;

    public MessageRepository(MongoDbContext ctx) => _col = ctx.Messages;

    public async Task<ChatMessage?> GetByIdAsync(string messageId)
    {
        var filter = Builders<ChatMessage>.Filter.And(
            Builders<ChatMessage>.Filter.Eq(m => m.Id, messageId),
            Builders<ChatMessage>.Filter.Eq(m => m.IsDeleted, false));

        return await _col.Find(filter).FirstOrDefaultAsync();
    }

    /// <summary>
    /// Keyset pagination using MongoDB ObjectId as the cursor.
    /// ObjectId embeds a Unix timestamp in its first 4 bytes, making it monotonically increasing
    /// and therefore suitable as a time-ordered cursor without a separate Timestamp field comparison.
    ///
    /// Query: ConversationId == X AND IsDeleted == false AND _id &lt; cursor
    /// Sort:  _id DESC (newest first)
    /// Index: (conversationId ASC, _id DESC) → see CreateIndexesAsync
    /// </summary>
    public async Task<IEnumerable<ChatMessage>> GetByConversationAsync(
        string  conversationId,
        string? beforeMessageId,
        int     pageSize)
    {
        var fb = Builders<ChatMessage>.Filter;

        var filter = fb.And(
            fb.Eq(m => m.ConversationId, conversationId),
            fb.Eq(m => m.IsDeleted, false));

        // Apply cursor: only return messages older than the cursor document
        if (!string.IsNullOrEmpty(beforeMessageId) && ObjectId.TryParse(beforeMessageId, out var cursorObjectId))
        {
            // Compare ObjectId strings lexicographically — MongoDB BSON comparison handles ObjectId correctly
            filter = fb.And(filter, fb.Lt(m => m.Id, cursorObjectId.ToString()));
        }

        return await _col
            .Find(filter)
            .SortByDescending(m => m.Id)   // ObjectId DESC = newest first
            .Limit(pageSize)
            .ToListAsync();
    }

    public async Task<ChatMessage> CreateAsync(ChatMessage message)
    {
        await _col.InsertOneAsync(message);
        return message;
    }

    /// <summary>
    /// Uses MongoDB UpdateMany with $push to efficiently bulk-add a ReadReceipt
    /// to all messages in the conversation that the user has not yet read.
    /// The compound index (conversationId, readBy.userId) makes this query efficient.
    /// </summary>
    public async Task MarkAsReadAsync(string conversationId, Guid userId)
    {
        var filter = Builders<ChatMessage>.Filter.And(
            Builders<ChatMessage>.Filter.Eq(m => m.ConversationId, conversationId),
            Builders<ChatMessage>.Filter.Eq(m => m.IsDeleted, false),
            // Exclude messages the user has already read
            Builders<ChatMessage>.Filter.Not(
                Builders<ChatMessage>.Filter.ElemMatch(
                    m => m.ReadBy,
                    r => r.UserId == userId)));

        var update = Builders<ChatMessage>.Update.Push(
            m => m.ReadBy,
            new ReadReceipt { UserId = userId, ReadAt = DateTime.UtcNow });

        await _col.UpdateManyAsync(filter, update);
    }

    public async Task SoftDeleteAsync(string messageId)
    {
        var filter = Builders<ChatMessage>.Filter.Eq(m => m.Id, messageId);
        var update = Builders<ChatMessage>.Update.Set(m => m.IsDeleted, true);
        await _col.UpdateOneAsync(filter, update);
    }

    /// <summary>
    /// Index strategy for the "messages" collection:
    ///
    /// 1. (conversationId ASC, _id DESC)
    ///    → Primary keyset pagination index. Covers the main GetByConversationAsync query.
    ///    → _id (ObjectId) carries embedded timestamp so no separate Timestamp index is needed.
    ///
    /// 2. (conversationId ASC, readBy.userId ASC)
    ///    → Optimizes MarkAsReadAsync bulk update (filter by conversation + readBy array element).
    ///
    /// 3. (conversationId ASC, senderId ASC)
    ///    → Optional: useful for "messages I sent in this conversation" queries.
    /// </summary>
    public async Task CreateIndexesAsync()
    {
        var models = new List<CreateIndexModel<ChatMessage>>
        {
            new(Builders<ChatMessage>.IndexKeys
                    .Ascending(m => m.ConversationId)
                    .Descending(m => m.Id),
                new CreateIndexOptions { Name = "idx_conv_id_desc" }),

            new(Builders<ChatMessage>.IndexKeys
                    .Ascending(m => m.ConversationId)
                    .Ascending("readBy.userId"),
                new CreateIndexOptions { Name = "idx_conv_readby_user" }),

            new(Builders<ChatMessage>.IndexKeys
                    .Ascending(m => m.ConversationId)
                    .Ascending(m => m.SenderId),
                new CreateIndexOptions { Name = "idx_conv_sender", Background = true }),
        };

        await _col.Indexes.CreateManyAsync(models);
    }
}
