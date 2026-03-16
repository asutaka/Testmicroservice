using Grpc.Core;
using UserService.Application.Interfaces;
using UserService.Contracts.Grpc;

namespace UserService.API.GrpcServices;

/// <summary>
/// gRPC service for internal communication from OrderService / BFF
/// </summary>
public sealed class UserGrpcService : UserGrpc.UserGrpcBase
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<UserGrpcService> _logger;

    public UserGrpcService(IUserRepository userRepository, ILogger<UserGrpcService> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public override async Task<GetUserByIdResponse> GetUserById(
        GetUserByIdRequest request,
        ServerCallContext context)
    {
        if (!Guid.TryParse(request.UserId, out var userId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid user ID format."));

        var user = await _userRepository.GetByIdAsync(userId, context.CancellationToken);

        if (user is null)
        {
            _logger.LogWarning("gRPC: User {UserId} not found", userId);
            return new GetUserByIdResponse { Found = false };
        }

        return new GetUserByIdResponse
        {
            Found = true,
            User = new UserMessage
            {
                Id = user.Id.ToString(),
                FirstName = user.FirstName,
                LastName = user.LastName,
                FullName = user.FullName,
                Email = user.Email.Value,
                Role = user.Role.ToString(),
                IsActive = user.IsActive
            }
        };
    }

    public override async Task<GetUserByIdResponse> GetUserByEmail(
        GetUserByEmailRequest request,
        ServerCallContext context)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, context.CancellationToken);

        if (user is null)
            return new GetUserByIdResponse { Found = false };

        return new GetUserByIdResponse
        {
            Found = true,
            User = new UserMessage
            {
                Id = user.Id.ToString(),
                FirstName = user.FirstName,
                LastName = user.LastName,
                FullName = user.FullName,
                Email = user.Email.Value,
                Role = user.Role.ToString(),
                IsActive = user.IsActive
            }
        };
    }

    public override async Task<UserExistsResponse> UserExists(
        UserExistsRequest request,
        ServerCallContext context)
    {
        if (!Guid.TryParse(request.UserId, out var userId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid user ID format."));

        var user = await _userRepository.GetByIdAsync(userId, context.CancellationToken);
        return new UserExistsResponse { Exists = user is not null && user.IsActive };
    }
}
