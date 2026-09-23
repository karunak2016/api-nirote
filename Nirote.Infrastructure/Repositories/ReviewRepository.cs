using System.Data;
using Dapper;
using Nirote.Application.Interfaces;
using Nirote.Domain.Entities;
using Nirote.Infrastructure.Data;

namespace Nirote.Infrastructure.Repositories;

public class ReviewRepository : IReviewRepository
{
    private readonly IDbConnectionFactory _db;
    public ReviewRepository(IDbConnectionFactory db) => _db = db;

    public async Task<IEnumerable<Review>> GetByProductAsync(int productId, bool approvedOnly)
    {
        using var conn = _db.Create();
        return await conn.QueryAsync<Review>(
            "sp_Reviews_GetByProduct",
            new { ProductId = productId, ApprovedOnly = approvedOnly },
            commandType: CommandType.StoredProcedure);
    }

    public async Task<IEnumerable<Review>> GetAllAsync()
    {
        using var conn = _db.Create();
        return await conn.QueryAsync<Review>(
            "sp_Reviews_GetAll",
            commandType: CommandType.StoredProcedure);
    }

    public async Task<IEnumerable<Review>> GetFeaturedAsync(int count)
    {
        using var conn = _db.Create();
        return await conn.QueryAsync<Review>(
            "SELECT TOP (@Count) Id, ProductId, UserId, UserName, Rating, Title, Body, IsApproved, CreatedAt FROM Reviews WHERE IsApproved = 1 ORDER BY Rating DESC, CreatedAt DESC",
            new { Count = count });
    }

    public async Task CreateAsync(Review review)
    {
        using var conn = _db.Create();
        await conn.ExecuteAsync(
            "sp_Reviews_Create",
            new
            {
                review.ProductId,
                review.UserId,
                review.UserName,
                review.Rating,
                review.Title,
                review.Body,
                review.IsApproved,
            },
            commandType: CommandType.StoredProcedure);
    }

    public async Task ApproveAsync(int id)
    {
        using var conn = _db.Create();
        await conn.ExecuteAsync(
            "sp_Reviews_Approve",
            new { Id = id },
            commandType: CommandType.StoredProcedure);
    }

    public async Task DeleteAsync(int id)
    {
        using var conn = _db.Create();
        await conn.ExecuteAsync(
            "sp_Reviews_Delete",
            new { Id = id },
            commandType: CommandType.StoredProcedure);
    }
}
