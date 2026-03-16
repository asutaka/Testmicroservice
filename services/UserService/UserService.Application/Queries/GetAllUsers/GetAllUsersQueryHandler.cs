using Dapper;
using MediatR;
using SharedKernel.Common;
using UserService.Application.DTOs;
using UserService.Application.Interfaces;

namespace UserService.Application.Queries.GetAllUsers;

public sealed class GetAllUsersQueryHandler : IRequestHandler<GetAllUsersQuery, Result<IEnumerable<UserDto>>>
{
    private readonly ISqlConnectionFactory _sqlConnectionFactory;

    public GetAllUsersQueryHandler(ISqlConnectionFactory sqlConnectionFactory) => _sqlConnectionFactory = sqlConnectionFactory;

    public async Task<Result<IEnumerable<UserDto>>> Handle(GetAllUsersQuery request, CancellationToken cancellationToken)
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
        ";

        var dtos = await connection.QueryAsync<UserDto>(sql);
        return Result<IEnumerable<UserDto>>.Success(dtos);
    }
}
