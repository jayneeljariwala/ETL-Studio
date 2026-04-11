using ETL.Domain.Common;
using ETL.Domain.Enums;

namespace ETL.Domain.Entities;

public sealed class EtlJobHistory : AuditableEntity
{
    public Guid EtlJobId { get; private set; }
    public EtlJob? EtlJob { get; private set; }
    public EtlJobStatus Status { get; private set; }
    public DateTimeOffset StartedAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }
    public int RecordsRead { get; private set; }
    public int RecordsTransformed { get; private set; }
    public int RecordsLoaded { get; private set; }
    public int RecordsFailed { get; private set; }
    public string? ErrorMessage { get; private set; }

    private EtlJobHistory()
    {
    }

    public static EtlJobHistory Started(Guid etlJobId, EtlJobStatus status)
    {
        if (etlJobId == Guid.Empty)
        {
            throw new DomainException("ETL job id is required.");
        }

        return new EtlJobHistory
        {
            Id = Guid.NewGuid(),
            EtlJobId = etlJobId,
            Status = status,
            StartedAtUtc = DateTimeOffset.UtcNow
        };
    }

    public void Complete(
        EtlJobStatus finalStatus,
        int recordsRead,
        int recordsTransformed,
        int recordsLoaded,
        int recordsFailed,
        string? errorMessage)
    {
        if (CompletedAtUtc.HasValue)
        {
            throw new DomainException("This ETL execution history entry is already completed.");
        }

        if (recordsRead < 0 || recordsTransformed < 0 || recordsLoaded < 0 || recordsFailed < 0)
        {
            throw new DomainException("Execution metrics cannot be negative.");
        }

        Status = finalStatus;
        RecordsRead = recordsRead;
        RecordsTransformed = recordsTransformed;
        RecordsLoaded = recordsLoaded;
        RecordsFailed = recordsFailed;
        ErrorMessage = string.IsNullOrWhiteSpace(errorMessage) ? null : errorMessage.Trim();
        CompletedAtUtc = DateTimeOffset.UtcNow;
        MarkUpdated();
    }
}
