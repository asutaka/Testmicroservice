using MediatR;
using SharedKernel.Common;
using UserService.Application.DTOs;

namespace UserService.Application.Queries.GetUser;

public record GetUserByIdQuery(Guid UserId) : IRequest<Result<UserDto>>;
