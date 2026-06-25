namespace Message.Application.Settings;

public class AgoraSettings
{
    public string AppId { get; init; } = string.Empty;
    public string AppCertificate { get; init; } = string.Empty;
    public uint TokenExpireSeconds { get; init; } = 3600;
}
