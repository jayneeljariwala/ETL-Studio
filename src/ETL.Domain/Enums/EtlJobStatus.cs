namespace ETL.Domain.Enums;

public enum EtlJobStatus
{
    Draft = 1,
    Queued = 2,
    Running = 3,
    Succeeded = 4,
    Failed = 5
}
