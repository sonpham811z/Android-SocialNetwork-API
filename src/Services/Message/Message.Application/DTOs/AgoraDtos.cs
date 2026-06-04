namespace Message.Application.DTOs;

public record AgoraTokenResponse(
    string Token,
    string AppId,
    string ChannelName,
    uint   ExpireAt);
