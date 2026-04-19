using ETL.Application.Interfaces.Repositories;
using ETL.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ETL.Infrastructure.Persistence.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _dbContext;

    public UserRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<ApplicationUser?> GetUserByIdentityIdAsync(string identityId, CancellationToken cancellationToken = default)
    {
        return _dbContext.UsersProfile
            .FirstOrDefaultAsync(x => x.IdentityId == identityId, cancellationToken);
    }

    public void AddUser(ApplicationUser user)
    {
        _dbContext.UsersProfile.Add(user);
    }
}
