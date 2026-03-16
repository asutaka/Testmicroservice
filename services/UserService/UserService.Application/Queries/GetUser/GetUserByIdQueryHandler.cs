using MediatR;
using SharedKernel.Common;
using UserService.Application.DTOs;
using UserService.Application.Interfaces;
using UserService.Domain.Entities;

namespace UserService.Application.Queries.GetUser;

public sealed class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, Result<UserDto>>
{
    private readonly IUserRepository _userRepository;

    public GetUserByIdQueryHandler(IUserRepository userRepository) => _userRepository = userRepository;

    public async Task<Result<UserDto>> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);

        return user is null
            ? Result<UserDto>.Failure($"User with ID '{request.UserId}' was not found.")
            : Result<UserDto>.Success(MapToDto(user));
    }

    private static UserDto MapToDto(User user) => new(
        user.Id,
        user.FirstName,
        user.LastName,
        user.FullName,
        user.Email.Value,
        user.Role,
        user.IsActive,
        user.CreatedAt,
        user.UpdatedAt);
}
