using System.Data;

namespace UserService.Application.Interfaces;

public interface ISqlConnectionFactory
{
    IDbConnection CreateConnection();
}
