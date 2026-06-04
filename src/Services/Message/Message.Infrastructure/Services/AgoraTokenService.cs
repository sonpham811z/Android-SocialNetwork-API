using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using Message.Application.Interfaces;
using Message.Application.Settings;
using Microsoft.Extensions.Options;

namespace Message.Infrastructure.Services;

/// <summary>
/// Generates Agora AccessToken2 ("007" format) for RTC voice/video calls.
/// Algorithm: https://github.com/AgoraIO/Tools/tree/master/DynamicKey/AgoraDynamicKey/csharp
/// </summary>
public class AgoraTokenService : IAgoraTokenService
{
    private const string Version = "007";

    // RTC privilege keys
    private const ushort PrivJoinChannel       = 1;
    private const ushort PrivPublishAudio      = 2;
    private const ushort PrivPublishVideo      = 3;
    private const ushort PrivPublishDataStream = 4;

    // Service type
    private const ushort ServiceTypeRtc = 1;

    private readonly AgoraSettings _settings;

    public AgoraTokenService(IOptions<AgoraSettings> options)
        => _settings = options.Value;

    public string GenerateRtcToken(string channelName, string uid, bool isPublisher)
    {
        var issuedTs = (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var expire   = issuedTs + _settings.TokenExpireSeconds;
        var salt     = (uint)Random.Shared.Next(1, int.MaxValue);

        // ── Pack message ──────────────────────────────────────────────────────
        using var msgStream = new MemoryStream();
        using (var bw = new BinaryWriter(msgStream, Encoding.UTF8, leaveOpen: true))
        {
            PackString(bw, _settings.AppId);
            bw.Write(issuedTs);
            bw.Write(expire);
            bw.Write(salt);

            // 1 service: RTC
            bw.Write((ushort)1);
            bw.Write(ServiceTypeRtc);

            // RTC service payload
            PackString(bw, channelName);
            PackString(bw, uid);

            var privileges = BuildPrivileges(isPublisher, expire);
            bw.Write((ushort)privileges.Count);
            foreach (var (key, value) in privileges)
            {
                bw.Write(key);
                bw.Write(value);
            }
        }
        var msgBytes = msgStream.ToArray();

        // ── Sign with HMAC-SHA256 ─────────────────────────────────────────────
        var certBytes = Encoding.UTF8.GetBytes(_settings.AppCertificate);
        using var hmac = new HMACSHA256(certBytes);
        var signature = hmac.ComputeHash(msgBytes);

        // ── Pack content: signature + message ─────────────────────────────────
        using var contentStream = new MemoryStream();
        using (var bw = new BinaryWriter(contentStream, Encoding.UTF8, leaveOpen: true))
        {
            PackBytes(bw, signature);
            PackBytes(bw, msgBytes);
        }

        // ── ZLib compress ─────────────────────────────────────────────────────
        var compressed = ZLibCompress(contentStream.ToArray());

        return Version + Convert.ToBase64String(compressed);
    }

    private static Dictionary<ushort, uint> BuildPrivileges(bool isPublisher, uint expire)
    {
        var p = new Dictionary<ushort, uint> { [PrivJoinChannel] = expire };
        if (isPublisher)
        {
            p[PrivPublishAudio]      = expire;
            p[PrivPublishVideo]      = expire;
            p[PrivPublishDataStream] = expire;
        }
        return p;
    }

    private static void PackString(BinaryWriter bw, string s)
    {
        var bytes = Encoding.UTF8.GetBytes(s);
        bw.Write((ushort)bytes.Length);
        bw.Write(bytes);
    }

    private static void PackBytes(BinaryWriter bw, byte[] bytes)
    {
        bw.Write((uint)bytes.Length);
        bw.Write(bytes);
    }

    private static byte[] ZLibCompress(byte[] data)
    {
        using var output = new MemoryStream();
        using (var zlib = new ZLibStream(output, CompressionLevel.BestCompression))
            zlib.Write(data);
        return output.ToArray();
    }
}
