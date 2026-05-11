using Friend.API.GrpcProtos;
using Friend.Domain.Interfaces;
using Grpc.Core;

namespace Friend.API.Grpc;

public class FriendshipGrpcService : FriendshipService.FriendshipServiceBase
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<FriendshipGrpcService> _logger;

    public FriendshipGrpcService(IUnitOfWork uow, ILogger<FriendshipGrpcService> logger)
    {
        _uow    = uow;
        _logger = logger;
    }

    public override async Task<CheckFriendshipResponse> CheckFriendship(
        CheckFriendshipRequest request, ServerCallContext context)
    {

        _logger.LogInformation("CheckFriendship Request details: {RequestData}", request.ToString());
        if (!Guid.TryParse(request.UserId1, out var userId1) ||
            !Guid.TryParse(request.UserId2, out var userId2))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument,
                "Both user IDs must be valid GUIDs."));
        }
        var areFriends = await _uow.Friendships.AreFriendsAsync(userId1, userId2);

        _logger.LogInformation("CheckFriendship({U1}, {U2}) → {Result}", userId1, userId2, areFriends);

        return new CheckFriendshipResponse { AreFriends = areFriends };
    }
}
