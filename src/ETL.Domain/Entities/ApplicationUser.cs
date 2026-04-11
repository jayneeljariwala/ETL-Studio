using ETL.Domain.Common;

namespace ETL.Domain.Entities;

public sealed class ApplicationUser : AuditableEntity
{
    private readonly List<EtlJob> _etlJobs = new();

    public string IdentityId { get; private set; } = default!;
    public string UserName { get; private set; } = default!;
    public string Email { get; private set; } = default!;
    public IReadOnlyCollection<EtlJob> EtlJobs => _etlJobs.AsReadOnly();

    private ApplicationUser()
    {
    }

    public static ApplicationUser Create(string identityId, string userName, string email)
    {
        if (string.IsNullOrWhiteSpace(identityId))
        {
            throw new DomainException("Identity id is required.");
        }

        if (string.IsNullOrWhiteSpace(userName))
        {
            throw new DomainException("User name is required.");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new DomainException("Email is required.");
        }

        return new ApplicationUser
        {
            Id = Guid.NewGuid(),
            IdentityId = identityId.Trim(),
            UserName = userName.Trim(),
            Email = email.Trim()
        };
    }

    public void UpdateProfile(string userName, string email)
    {
        if (string.IsNullOrWhiteSpace(userName))
        {
            throw new DomainException("User name is required.");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new DomainException("Email is required.");
        }

        UserName = userName.Trim();
        Email = email.Trim();
        MarkUpdated();
    }
}
