using Dapper;
using MediatR;
using SharedKernel.Common;
using UserService.Application.DTOs;
using UserService.Application.Interfaces;

namespace UserService.Application.Queries.GetUser;

public sealed class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, Result<UserDto>>
{
    private readonly ISqlConnectionFactory _sqlConnectionFactory;

    public GetUserByIdQueryHandler(ISqlConnectionFactory sqlConnectionFactory) => _sqlConnectionFactory = sqlConnectionFactory;

    public async Task<Result<UserDto>> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        using var connection = _sqlConnectionFactory.CreateConnection();
        const string sql = @"
            SELECT 
                ""Id"", 
                ""FirstName"", 
                ""LastName"", 
                ""FirstName"" || ' ' || ""LastName"" AS ""FullName"", 
                ""Email"", 
                CAST(""Role"" AS integer) AS ""Role"", 
                ""IsActive"", 
                ""CreatedAt"", 
                ""UpdatedAt""
            FROM ""Users""
            WHERE ""Id"" = @Id
        ";

        var user = await connection.QueryFirstOrDefaultAsync<UserDto>(sql, new { Id = request.UserId });

        return user is null
            ? Result<UserDto>.Failure($"User with ID '{request.UserId}' was not found.")
            : Result<UserDto>.Success(user);
    }
}
