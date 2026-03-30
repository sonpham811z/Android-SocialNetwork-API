using Message.Domain.Entities;

namespace Message.Domain.Interfaces;

public interface IConversationRepository
{
    Task<Conversation?>            GetByIdAsync(string conversationId);
    Task<Conversation?>            GetOneToOneAsync(Guid user1, Guid user2);
    Task<IEnumerable<Conversation>> GetByUserIdAsync(Guid userId);
    Task<Conversation>             CreateAsync(Conversation conversation);
    Task                           UpdateLastMessageAsync(string conversationId, LastMessageInfo lastMessage);
    Task                           AddMemberAsync(string conversationId, Guid userId);
}

public interface IMessageRepository
{
    Task<ChatMessage?> GetByIdAsync(string messageId);

    /// <summary>
    /// Keyset pagination over a conversation's message history.
    /// Returns up to <paramref name="pageSize"/> messages with ObjectId &lt; <paramref name="beforeMessageId"/>
    /// (i.e., older than the cursor), sorted newest-first (_id DESC).
    /// Pass <c>null</c> for <paramref name="beforeMessageId"/> to fetch the latest page.
    /// </summary>
    Task<IEnumerable<ChatMessage>> GetByConversationAsync(
        string  conversationId,
        string? beforeMessageId,
        int     pageSize);

    Task<ChatMessage> CreateAsync(ChatMessage message);

    /// <summary>
    /// Atomically pushes a ReadReceipt to all unread messages in the conversation for the given user.
    /// Uses a bulk UpdateMany with $push on the readBy array.
    /// </summary>
    Task MarkAsReadAsync(string conversationId, Guid userId);

    Task SoftDeleteAsync(string messageId);

    /// <summary>
    /// Creates compound MongoDB indexes for optimal keyset pagination and read-receipt query performance.
    /// Should be called once on application startup.
    /// </summary>
    Task CreateIndexesAsync();
}
