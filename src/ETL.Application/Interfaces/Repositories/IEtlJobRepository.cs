using ETL.Domain.Entities;
using ETL.Domain.Enums;

namespace ETL.Application.Interfaces.Repositories;

public interface IEtlJobRepository
{
    Task<int> GetTotalJobsCountAsync(CancellationToken cancellationToken = default);
    Task<int> GetActiveJobsCountAsync(CancellationToken cancellationToken = default);
    Task<int> GetSuccessfulRunsCountAsync(CancellationToken cancellationToken = default);
    Task<int> GetFailedRunsCountAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EtlJobHistory>> GetRecentJobRunsAsync(int count, CancellationToken cancellationToken = default);
    
    Task<IReadOnlyList<EtlJob>> GetAllJobsAsync(CancellationToken cancellationToken = default);
    Task<EtlJob?> GetJobByIdAsync(Guid id, bool trackChanges = false, CancellationToken cancellationToken = default);
    Task<EtlJob?> GetJobWithDetailsByIdAsync(Guid id, bool trackChanges = false, CancellationToken cancellationToken = default);
    void AddJob(EtlJob job);

    Task<bool> JobExistsAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EtlJobHistory>> GetJobHistoryAsync(Guid jobId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EtlJobHistory>> GetJobErrorHistoryAsync(Guid jobId, CancellationToken cancellationToken = default);
    
    Task<EtlJob?> GetJobForExecutionAsync(Guid id, CancellationToken cancellationToken = default);
}
