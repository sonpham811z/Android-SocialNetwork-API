using Message.Application.Interfaces;
using Message.Infrastructure.GrpcProtos;
using Microsoft.Extensions.Logging;

namespace Message.Infrastructure.Services;

/// <summary>
/// gRPC client that calls Friend Service to verify friendship status between two users.
///
/// Prerequisites (Friend Service side):
///   1. Add packages: Grpc.AspNetCore, Google.Protobuf, Grpc.Tools
///   2. Copy Protos/friendship.proto with GrpcServices="Server"
///   3. Implement FriendshipService.FriendshipServiceBase:
///      - Query IFriendRepository.AreFriendsAsync(userId1, userId2)
///   4. Register in Program.cs: app.MapGrpcService&lt;FriendshipGrpcService&gt;();
///   5. Expose gRPC endpoint (default port 5001 or configure in appsettings)
/// </summary>
public class GrpcFriendServiceClient : IFriendServiceClient
{
    private readonly FriendshipService.FriendshipServiceClient _grpcClient;
    private readonly ILogger<GrpcFriendServiceClient>          _logger;

    public GrpcFriendServiceClient(
        FriendshipService.FriendshipServiceClient grpcClient,
        ILogger<GrpcFriendServiceClient>          logger)
    {
        _grpcClient = grpcClient;
        _logger     = logger;
    }

    public async Task<bool> AreFriendsAsync(Guid userId1, Guid userId2, CancellationToken ct = default)
    {
        try
        {
            var request = new CheckFriendshipRequest
            {
                UserId1 = userId1.ToString(),
                UserId2 = userId2.ToString()
            };

            var response = await _grpcClient.CheckFriendshipAsync(
                request, cancellationToken: ct);

            return response.AreFriends;
        }
        catch (Grpc.Core.RpcException ex)
        {
            _logger.LogError(ex,
                "gRPC CheckFriendship failed for users {User1}/{User2} — Status: {Status}",
                userId1, userId2, ex.StatusCode);

            throw new InvalidOperationException(
                "Unable to verify friendship status. Friend Service is unavailable.", ex);
        }
    }
}
