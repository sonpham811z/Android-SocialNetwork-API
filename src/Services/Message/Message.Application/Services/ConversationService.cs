using Message.Application.DTOs;
using Message.Application.Interfaces;
using Message.Domain.Entities;
using Message.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Message.Application.Services;

public class ConversationService : IConversationService
{
    private readonly IConversationRepository   _convRepo;
    private readonly IFriendServiceClient      _friendClient;
    private readonly IConversationCacheService _cache;
    private readonly ILogger<ConversationService> _logger;

    public ConversationService(
        IConversationRepository      convRepo,
        IFriendServiceClient         friendClient,
        IConversationCacheService    cache,
        ILogger<ConversationService> logger)
    {
        _convRepo     = convRepo;
        _friendClient = friendClient;
        _cache        = cache;
        _logger       = logger;
    }

    public async Task<ConversationDto> CreateOneToOneAsync(Guid currentUserId, Guid targetUserId)
    {
        if (currentUserId == targetUserId)
            throw new ArgumentException("Cannot create a conversation with yourself.");

        // 1. Friendship check via gRPC → Friend Service
        var areFriends = await _friendClient.AreFriendsAsync(currentUserId, targetUserId);
        if (!areFriends)
            throw new InvalidOperationException("Cannot start a conversation with a non-friend user.");

        // 2. Return existing 1-1 conversation if one already exists
        var existing = await _convRepo.GetOneToOneAsync(currentUserId, targetUserId);
        if (existing is not null)
            return MapToDto(existing);

        // 3. Create new conversation
        var conversation = Conversation.CreateOneToOne(currentUserId, targetUserId);
        var created = await _convRepo.CreateAsync(conversation);

        _logger.LogInformation(
            "Created 1-1 conversation {ConversationId} between {User1} and {User2}",
            created.Id, currentUserId, targetUserId);

        return MapToDto(created);
    }

    public async Task<ConversationDto> CreateGroupAsync(Guid currentUserId, CreateGroupConversationDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.GroupName))
            throw new ArgumentException("Group name cannot be empty.");

        if (dto.Members.Count < 2)
            throw new ArgumentException("A group requires at least 2 other members besides the creator.");

        // Ensure creator is always included
        var allMembers = dto.Members.ToHashSet();
        allMembers.Add(currentUserId);

        var conversation = Conversation.CreateGroup(currentUserId, [.. allMembers], dto.GroupName);
        var created = await _convRepo.CreateAsync(conversation);

        _logger.LogInformation(
            "Created group conversation {ConversationId} '{GroupName}' by {CreatedBy} with {MemberCount} members",
            created.Id, dto.GroupName, currentUserId, created.Members.Count);

        return MapToDto(created);
    }

    public async Task<IEnumerable<ConversationDto>> GetUserConversationsAsync(Guid userId)
    {
        var conversations = await _convRepo.GetByUserIdAsync(userId);
        return conversations.Select(MapToDto);
    }

    public async Task<ConversationDto?> GetConversationAsync(string conversationId, Guid userId)
    {
        // Try Redis cache first to avoid MongoDB round-trip on hot paths
        var cached = await _cache.GetAsync(conversationId);
        if (cached is not null)
        {
            if (!cached.Members.Contains(userId))
                throw new UnauthorizedAccessException("You are not a member of this conversation.");
            return cached;
        }

        var conversation = await _convRepo.GetByIdAsync(conversationId);
        if (conversation is null) return null;

        if (!conversation.HasMember(userId))
            throw new UnauthorizedAccessException("You are not a member of this conversation.");

        var dto = MapToDto(conversation);
        await _cache.SetAsync(dto);
        return dto;
    }

    // ── Mapping ──────────────────────────────────────────────────────────────

    private static ConversationDto MapToDto(Conversation c) => new()
    {
        Id          = c.Id,
        Type        = c.Type,
        Members     = c.Members,
        GroupName   = c.GroupName,
        LastMessage = c.LastMessage is null ? null : new LastMessageDto
        {
            MessageId = c.LastMessage.MessageId,
            SenderId  = c.LastMessage.SenderId,
            Content   = c.LastMessage.Content,
            Timestamp = c.LastMessage.Timestamp
        },
        UpdatedAt = c.UpdatedAt
    };
}
