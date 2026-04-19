using ETL.Domain.Entities;

namespace ETL.Application.Interfaces.Repositories;

public interface IUserRepository
{
    Task<ApplicationUser?> GetUserByIdentityIdAsync(string identityId, CancellationToken cancellationToken = default);
    void AddUser(ApplicationUser user);
}
