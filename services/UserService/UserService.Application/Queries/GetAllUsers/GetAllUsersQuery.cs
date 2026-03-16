using MediatR;
using SharedKernel.Common;
using UserService.Application.DTOs;

namespace UserService.Application.Queries.GetAllUsers;

public record GetAllUsersQuery : IRequest<Result<IEnumerable<UserDto>>>;
