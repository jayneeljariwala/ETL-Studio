using ETL.Domain.Entities;
using ETL.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ETL.Infrastructure.Persistence;

public sealed class ApplicationDbContext : IdentityDbContext<AppIdentityUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<ApplicationUser> UsersProfile => Set<ApplicationUser>();
    public DbSet<EtlJob> EtlJobs => Set<EtlJob>();
    public DbSet<EtlJobHistory> EtlJobHistory => Set<EtlJobHistory>();
    public DbSet<FieldMapping> FieldMappings => Set<FieldMapping>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
