using Message.Domain.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Message.Domain.Entities;

public class Conversation
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = default!;

    [BsonRepresentation(BsonType.String)]
    public ConversationType Type { get; set; }

    public List<Guid> Members { get; set; } = [];

    public LastMessageInfo? LastMessage { get; set; }

    public string? GroupName { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    // ── Factory methods ──────────────────────────────────────────────────────

    public static Conversation CreateOneToOne(Guid user1, Guid user2)
        => new()
        {
            Type      = ConversationType.OneToOne,
            Members   = [user1, user2],
            CreatedBy = user1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

    public static Conversation CreateGroup(Guid createdBy, List<Guid> members, string groupName)
        => new()
        {
            Type      = ConversationType.Group,
            Members   = members,
            GroupName = groupName,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

    // ── Methods ──────────────────────────────────────────────────────────────

    public void UpdateLastMessage(LastMessageInfo lastMessage)
    {
        LastMessage = lastMessage;
        UpdatedAt   = DateTime.UtcNow;
    }

    public bool HasMember(Guid userId) => Members.Contains(userId);
}

public class LastMessageInfo
{
    [BsonRepresentation(BsonType.ObjectId)]
    public string   MessageId { get; set; } = default!;
    public Guid     SenderId  { get; set; }
    public string   Content   { get; set; } = default!;
    public DateTime Timestamp { get; set; }
}
