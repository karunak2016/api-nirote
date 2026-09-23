using System.Data;
using Dapper;
using Nirote.Domain.Entities;
using Nirote.Infrastructure.Data;

namespace Nirote.Infrastructure.Repositories;

public class UserRepository
{
    private readonly IDbConnectionFactory _db;

    public UserRepository(IDbConnectionFactory db) => _db = db;

    public async Task<User?> GetByEmailAsync(string email)
    {
        using var conn = _db.Create();
        return await conn.QuerySingleOrDefaultAsync<User>(
            "sp_Users_GetByEmail",
            new { Email = email },
            commandType: CommandType.StoredProcedure);
    }

    public async Task<User?> GetByIdAsync(int userId)
    {
        using var conn = _db.Create();
        return await conn.QuerySingleOrDefaultAsync<User>(
            "sp_Users_GetById",
            new { UserId = userId },
            commandType: CommandType.StoredProcedure);
    }

    public async Task<int> CreateAsync(User user)
    {
        using var conn = _db.Create();
        var p = new DynamicParameters();
        p.Add("@Name", user.Name);
        p.Add("@Email", user.Email);
        p.Add("@PasswordHash", user.PasswordHash);
        p.Add("@Phone", user.Phone);
        p.Add("@Role", user.Role.ToString());
        p.Add("@NewUserId", dbType: DbType.Int32, direction: ParameterDirection.Output);

        await conn.ExecuteAsync("sp_Users_Create", p, commandType: CommandType.StoredProcedure);
        return p.Get<int>("@NewUserId");
    }

    public async Task<User?> GetByPhoneAsync(string phone)
    {
        using var conn = _db.Create();
        return await conn.QuerySingleOrDefaultAsync<User>(
            "sp_Users_GetByPhone",
            new { Phone = phone },
            commandType: CommandType.StoredProcedure);
    }

    public async Task UpdateProfileAsync(int userId, string name, string email, string? phone)
    {
        using var conn = _db.Create();
        await conn.ExecuteAsync(
            "sp_Users_UpdateProfile",
            new { UserId = userId, Name = name, Email = email, Phone = string.IsNullOrWhiteSpace(phone) ? null : phone },
            commandType: System.Data.CommandType.StoredProcedure);
    }

    public async Task SetPasswordAsync(int userId, string passwordHash)
    {
        using var conn = _db.Create();
        await conn.ExecuteAsync(
            "sp_Users_SetPassword",
            new { UserId = userId, PasswordHash = passwordHash },
            commandType: CommandType.StoredProcedure);
    }

    public async Task<(IEnumerable<User> Items, int Total)> GetAllAsync(string? role, string? searchTerm, int page, int pageSize)
    {
        using var conn = _db.Create();
        var p = new DynamicParameters();
        p.Add("@Role", role);
        p.Add("@SearchTerm", searchTerm);
        p.Add("@Page", page);
        p.Add("@PageSize", pageSize);
        p.Add("@TotalCount", dbType: DbType.Int32, direction: ParameterDirection.Output);

        var items = await conn.QueryAsync<User>("sp_Users_GetAll", p, commandType: CommandType.StoredProcedure);
        return (items, p.Get<int>("@TotalCount"));
    }

    public async Task<IEnumerable<string>> GetAllCustomerEmailsAsync()
    {
        using var conn = _db.Create();
        return await conn.QueryAsync<string>(
            "SELECT Email FROM Users WHERE Role = 'Customer' AND Email IS NOT NULL AND Email <> ''",
            commandType: CommandType.Text);
    }

    public async Task<string?> GetEmailByUserIdAsync(int userId)
    {
        using var conn = _db.Create();
        return await conn.ExecuteScalarAsync<string>(
            "SELECT Email FROM Users WHERE Id = @UserId",
            new { UserId = userId },
            commandType: CommandType.Text);
    }
}
