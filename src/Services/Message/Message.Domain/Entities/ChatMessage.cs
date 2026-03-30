using Message.Domain.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Message.Domain.Entities;

/// <summary>
/// A single chat message within a Conversation.
/// Named ChatMessage to avoid namespace conflict with the project prefix "Message.*".
/// </summary>
public class ChatMessage
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = default!;

    [BsonRepresentation(BsonType.ObjectId)]
    public string ConversationId { get; set; } = default!;

    public Guid   SenderId { get; set; }
    public string Content  { get; set; } = default!;

    [BsonRepresentation(BsonType.String)]
    public MessageType Type { get; set; } = MessageType.Text;

    /// <summary>UTC timestamp of message creation. Also embedded in the ObjectId for keyset pagination.</summary>
    public DateTime Timestamp { get; set; }

    /// <summary>Users who have read this message, including the sender (auto-added on creation).</summary>
    public List<ReadReceipt> ReadBy { get; set; } = [];

    public bool IsDeleted { get; set; }

    // ── Factory ──────────────────────────────────────────────────────────────

    public static ChatMessage Create(
        string     conversationId,
        Guid       senderId,
        string     content,
        MessageType type = MessageType.Text)
        => new()
        {
            ConversationId = conversationId,
            SenderId       = senderId,
            Content        = content,
            Type           = type,
            Timestamp      = DateTime.UtcNow,
            // Sender automatically marks their own message as read
            ReadBy         = [new ReadReceipt { UserId = senderId, ReadAt = DateTime.UtcNow }],
            IsDeleted      = false
        };

    // ── Methods ──────────────────────────────────────────────────────────────

    public void AddReadReceipt(Guid userId)
    {
        if (!ReadBy.Any(r => r.UserId == userId))
            ReadBy.Add(new ReadReceipt { UserId = userId, ReadAt = DateTime.UtcNow });
    }

    public void SoftDelete() => IsDeleted = true;
}

public class ReadReceipt
{
    public Guid     UserId { get; set; }
    public DateTime ReadAt { get; set; }
}
