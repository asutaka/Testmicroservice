using MediatR;
using SharedKernel.Common;
using UserService.Application.DTOs;
using UserService.Domain.Entities;

namespace UserService.Application.Commands.CreateUser;

public record CreateUserCommand(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    UserRole Role = UserRole.Customer
) : IRequest<Result<UserDto>>;
