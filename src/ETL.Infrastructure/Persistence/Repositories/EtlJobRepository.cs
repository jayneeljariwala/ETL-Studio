using ETL.Application.Interfaces.Repositories;
using ETL.Domain.Entities;
using ETL.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ETL.Infrastructure.Persistence.Repositories;

public sealed class EtlJobRepository : IEtlJobRepository
{
    private readonly ApplicationDbContext _dbContext;

    public EtlJobRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<int> GetTotalJobsCountAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.EtlJobs.CountAsync(cancellationToken);
    }

    public Task<int> GetTotalJobsCountByOwnerAsync(Guid ownerId, CancellationToken cancellationToken = default)
    {
        return _dbContext.EtlJobs.CountAsync(x => x.CreatedByUserId == ownerId, cancellationToken);
    }

    public Task<int> GetActiveJobsCountAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.EtlJobs.CountAsync(x => x.IsActive, cancellationToken);
    }

    public Task<int> GetActiveJobsCountByOwnerAsync(Guid ownerId, CancellationToken cancellationToken = default)
    {
        return _dbContext.EtlJobs.CountAsync(x => x.IsActive && x.CreatedByUserId == ownerId, cancellationToken);
    }

    public Task<int> GetSuccessfulRunsCountAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.EtlJobHistory.CountAsync(x => x.Status == EtlJobStatus.Succeeded, cancellationToken);
    }

    public Task<int> GetSuccessfulRunsCountByOwnerAsync(Guid ownerId, CancellationToken cancellationToken = default)
    {
        return _dbContext.EtlJobHistory.CountAsync(x => x.Status == EtlJobStatus.Succeeded && x.EtlJob!.CreatedByUserId == ownerId, cancellationToken);
    }

    public Task<int> GetFailedRunsCountAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.EtlJobHistory.CountAsync(x => x.Status == EtlJobStatus.Failed, cancellationToken);
    }

    public Task<int> GetFailedRunsCountByOwnerAsync(Guid ownerId, CancellationToken cancellationToken = default)
    {
        return _dbContext.EtlJobHistory.CountAsync(x => x.Status == EtlJobStatus.Failed && x.EtlJob!.CreatedByUserId == ownerId, cancellationToken);
    }

    public async Task<IReadOnlyList<EtlJobHistory>> GetRecentJobRunsAsync(int count, CancellationToken cancellationToken = default)
    {
        return await _dbContext.EtlJobHistory
            .AsNoTracking()
            .Include(x => x.EtlJob)
            .OrderByDescending(x => x.StartedAtUtc)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EtlJobHistory>> GetRecentJobRunsByOwnerAsync(int count, Guid ownerId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.EtlJobHistory
            .AsNoTracking()
            .Include(x => x.EtlJob)
            .Where(x => x.EtlJob!.CreatedByUserId == ownerId)
            .OrderByDescending(x => x.StartedAtUtc)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EtlJob>> GetAllJobsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.EtlJobs
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EtlJob>> GetAllJobsByOwnerAsync(Guid ownerId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.EtlJobs
            .AsNoTracking()
            .Where(x => x.CreatedByUserId == ownerId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public Task<EtlJob?> GetJobByIdAsync(Guid id, bool trackChanges = false, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.EtlJobs.AsQueryable();
        if (!trackChanges)
        {
            query = query.AsNoTracking();
        }

        return query.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<EtlJob?> GetJobByIdForOwnerAsync(Guid id, Guid ownerId, bool trackChanges = false, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.EtlJobs.AsQueryable();
        if (!trackChanges)
        {
            query = query.AsNoTracking();
        }

        return query.FirstOrDefaultAsync(x => x.Id == id && x.CreatedByUserId == ownerId, cancellationToken);
    }

    public Task<EtlJob?> GetJobWithDetailsByIdAsync(Guid id, bool trackChanges = false, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.EtlJobs
            .Include(x => x.FieldMappings)
            .AsQueryable();

        if (!trackChanges)
        {
            query = query.AsNoTracking();
        }

        return query.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<EtlJob?> GetJobWithDetailsByIdForOwnerAsync(Guid id, Guid ownerId, bool trackChanges = false, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.EtlJobs
            .Include(x => x.FieldMappings)
            .ThenInclude(m => m.TransformationSteps)
            .AsQueryable();

        if (!trackChanges)
        {
            query = query.AsNoTracking();
        }

        return query.FirstOrDefaultAsync(x => x.Id == id && x.CreatedByUserId == ownerId, cancellationToken);
    }

    public void AddJob(EtlJob job)
    {
        _dbContext.EtlJobs.Add(job);
    }

    public Task<bool> JobExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.EtlJobs
            .AsNoTracking()
            .AnyAsync(x => x.Id == id, cancellationToken);
    }

    public Task<bool> JobBelongsToOwnerAsync(Guid id, Guid ownerId, CancellationToken cancellationToken = default)
    {
        return _dbContext.EtlJobs
            .AsNoTracking()
            .AnyAsync(x => x.Id == id && x.CreatedByUserId == ownerId, cancellationToken);
    }

    public async Task<IReadOnlyList<EtlJobHistory>> GetJobHistoryAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.EtlJobHistory
            .AsNoTracking()
            .Where(x => x.EtlJobId == jobId)
            .OrderByDescending(x => x.StartedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EtlJobHistory>> GetJobHistoryForOwnerAsync(Guid jobId, Guid ownerId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.EtlJobHistory
            .AsNoTracking()
            .Where(x => x.EtlJobId == jobId && x.EtlJob!.CreatedByUserId == ownerId)
            .OrderByDescending(x => x.StartedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EtlJobHistory>> GetJobErrorHistoryAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.EtlJobHistory
            .AsNoTracking()
            .Where(x => x.EtlJobId == jobId && x.Status == EtlJobStatus.Failed)
            .OrderByDescending(x => x.StartedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EtlJobHistory>> GetJobErrorHistoryForOwnerAsync(Guid jobId, Guid ownerId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.EtlJobHistory
            .AsNoTracking()
            .Where(x => x.EtlJobId == jobId && x.Status == EtlJobStatus.Failed && x.EtlJob!.CreatedByUserId == ownerId)
            .OrderByDescending(x => x.StartedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public Task<EtlJob?> GetJobForExecutionAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.EtlJobs
            .Include(x => x.FieldMappings)
            .Include(x => x.JobHistory)
            .Include(x => x.CreatedByUser)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<EtlJob?> GetJobForExecutionByOwnerAsync(Guid id, Guid ownerId, CancellationToken cancellationToken = default)
    {
        return _dbContext.EtlJobs
            .Include(x => x.FieldMappings)
            .Include(x => x.JobHistory)
            .FirstOrDefaultAsync(x => x.Id == id && x.CreatedByUserId == ownerId, cancellationToken);
    }
}
