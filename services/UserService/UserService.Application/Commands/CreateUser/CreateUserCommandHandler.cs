using EventBus.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using SharedKernel.Common;
using UserService.Application.DTOs;
using UserService.Application.Interfaces;
using UserService.Contracts.Events;
using UserService.Domain.Entities;
using UserService.Domain.ValueObjects;

namespace UserService.Application.Commands.CreateUser;

public sealed class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, Result<UserDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IEventBus _eventBus;
    private readonly ILogger<CreateUserCommandHandler> _logger;

    public CreateUserCommandHandler(
        IUserRepository userRepository,
        IEventBus eventBus,
        ILogger<CreateUserCommandHandler> logger)
    {
        _userRepository = userRepository;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task<Result<UserDto>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var emailExists = await _userRepository.ExistsWithEmailAsync(request.Email, cancellationToken);
            if (emailExists)
                return Result<UserDto>.Failure($"Email '{request.Email}' is already in use.");

            var email = Email.From(request.Email);
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            var user = User.Create(email, request.FirstName, request.LastName, passwordHash, request.Role);

            await _userRepository.AddAsync(user, cancellationToken);
            await _userRepository.SaveChangesAsync(cancellationToken);

            // Publish integration event to notify other services
            await _eventBus.PublishAsync(new UserCreatedIntegrationEvent(
                user.Id, user.Email.Value, user.FullName), cancellationToken);

            _logger.LogInformation("User created successfully: {UserId} ({Email})", user.Id, user.Email.Value);

            return Result<UserDto>.Success(MapToDto(user));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user with email: {Email}", request.Email);
            return Result<UserDto>.Failure($"Failed to create user: {ex.Message}");
        }
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
