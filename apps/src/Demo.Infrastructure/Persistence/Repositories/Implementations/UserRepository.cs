namespace Demo.Infrastructure.Persistence.Repositories.Implementations;

public class UserRepository : BaseRepository<Usuario>, IUserRepository
{
    public UserRepository(TestContext context) : base(context)
    {
    }

    public async Task<PagedResult<Usuario>> GetPaginatedUsersAsync(PaginationRequest pagination)
    {
        IQueryable<Usuario> query = _dbSet;

        query = query.OrderBy(u => u.Username);

        // Get total count for pagination metadata
        var totalCount = await query.CountAsync();

        // Apply pagination
        var items = await query
            .Skip((pagination.PageIndex - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync();

        return new PagedResult<Usuario>
        {
            Items = items,
            PageIndex = pagination.PageIndex,
            PageSize = pagination.PageSize,
            TotalCount = totalCount
        };
    }
}

