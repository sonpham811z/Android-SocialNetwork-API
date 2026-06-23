using Message.Domain.Enums;

namespace Message.Application.DTOs;

// ── Conversation DTOs ─────────────────────────────────────────────────────────

public class ConversationDto
{
    public string           Id          { get; init; } = default!;
    public ConversationType Type        { get; init; }
    public List<Guid>       Members     { get; init; } = [];
    public string?          GroupName   { get; init; }
    public LastMessageDto?  LastMessage { get; init; }
    public DateTime         UpdatedAt   { get; init; }
}

public class LastMessageDto
{
    public string      MessageId { get; init; } = default!;
    public Guid        SenderId  { get; init; }
    public string      Content   { get; init; } = default!;
    public MessageType Type      { get; init; } = MessageType.Text;
    public DateTime    Timestamp { get; init; }
}

public class CreateOneToOneConversationDto
{
    public Guid TargetUserId { get; init; }
}

public class CreateGroupConversationDto
{
    public string     GroupName { get; init; } = default!;
    public List<Guid> Members   { get; init; } = [];
}

// ── Message DTOs ──────────────────────────────────────────────────────────────

public class MessageDto
{
    public string               Id             { get; init; } = default!;
    public string               ConversationId { get; init; } = default!;
    public Guid                 SenderId       { get; init; }
    public string               Content        { get; init; } = default!;
    public MessageType          Type           { get; init; }
    public DateTime             Timestamp      { get; init; }
    public List<ReadReceiptDto> ReadBy         { get; init; } = [];
}

public class ReadReceiptDto
{
    public Guid     UserId { get; init; }
    public DateTime ReadAt { get; init; }
}

/// <summary>REST API payload for sending a message.</summary>
public class SendMessageDto
{
    public string      ConversationId { get; init; } = default!;
    public string      Content        { get; init; } = default!;
    public MessageType Type           { get; init; } = MessageType.Text;
}

/// <summary>SignalR hub payload for sending a message (client → server).</summary>
public class SendMessageHubRequest
{
    public string      ConversationId { get; init; } = default!;
    public string      Content        { get; init; } = default!;
    public MessageType Type           { get; init; } = MessageType.Text;
}

/// <summary>Response payload for an uploaded chat media attachment (image).</summary>
public class MediaUploadResponseDto
{
    /// <summary>Public Cloudinary URL — used as the <see cref="MessageDto.Content"/> of an Image message.</summary>
    public string Url      { get; init; } = default!;
    public string PublicId { get; init; } = default!;
}

/// <summary>
/// Paginated message history response using keyset pagination.
/// Use <see cref="NextCursor"/> as the <c>beforeMessageId</c> parameter in the next request.
/// </summary>
public class MessagePageDto
{
    public IEnumerable<MessageDto> Messages    { get; init; } = [];
    /// <summary>Pass this as <c>beforeMessageId</c> to load older messages. Null when no more pages exist.</summary>
    public string?                 NextCursor  { get; init; }
    public bool                    HasMore     { get; init; }
}

/// <summary>Read receipt event broadcast via SignalR when a user reads a conversation.</summary>
public class ReadReceiptEventDto
{
    public string   ConversationId { get; init; } = default!;
    public Guid     ReaderId       { get; init; }
    public DateTime ReadAt         { get; init; }
}

// ── Common ────────────────────────────────────────────────────────────────────

public class ApiResponse<T>
{
    public bool   Success { get; init; }
    public string Message { get; init; } = default!;
    public T?     Data    { get; init; }

    public static ApiResponse<T> Ok(T data, string message = "Success")
        => new() { Success = true, Message = message, Data = data };

    public static ApiResponse<T> Fail(string message)
        => new() { Success = false, Message = message };
}
