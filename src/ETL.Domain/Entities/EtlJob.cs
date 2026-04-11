using ETL.Domain.Common;
using ETL.Domain.Enums;

namespace ETL.Domain.Entities;

public sealed class EtlJob : AuditableEntity
{
    private readonly List<FieldMapping> _fieldMappings = new();
    private readonly List<EtlJobHistory> _jobHistory = new();

    public string Name { get; private set; } = default!;
    public string Description { get; private set; } = string.Empty;
    public DataSourceType SourceType { get; private set; }
    public string SourceConfigurationJson { get; private set; } = default!;
    public DataDestinationType DestinationType { get; private set; }
    public string DestinationConfigurationJson { get; private set; } = default!;
    public LoadStrategy LoadStrategy { get; private set; }
    public EtlJobStatus CurrentStatus { get; private set; } = EtlJobStatus.Draft;
    public bool IsActive { get; private set; } = true;
    public Guid CreatedByUserId { get; private set; }
    public ApplicationUser? CreatedByUser { get; private set; }

    public IReadOnlyCollection<FieldMapping> FieldMappings => _fieldMappings.AsReadOnly();
    public IReadOnlyCollection<EtlJobHistory> JobHistory => _jobHistory.AsReadOnly();

    private EtlJob()
    {
    }

    public static EtlJob Create(
        string name,
        string description,
        DataSourceType sourceType,
        string sourceConfigurationJson,
        DataDestinationType destinationType,
        string destinationConfigurationJson,
        LoadStrategy loadStrategy,
        Guid createdByUserId)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("ETL job name is required.");
        }

        if (string.IsNullOrWhiteSpace(sourceConfigurationJson))
        {
            throw new DomainException("Source configuration is required.");
        }

        if (string.IsNullOrWhiteSpace(destinationConfigurationJson))
        {
            throw new DomainException("Destination configuration is required.");
        }

        if (createdByUserId == Guid.Empty)
        {
            throw new DomainException("Created by user id is required.");
        }

        return new EtlJob
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Description = description?.Trim() ?? string.Empty,
            SourceType = sourceType,
            SourceConfigurationJson = sourceConfigurationJson.Trim(),
            DestinationType = destinationType,
            DestinationConfigurationJson = destinationConfigurationJson.Trim(),
            LoadStrategy = loadStrategy,
            CreatedByUserId = createdByUserId
        };
    }

    public void UpdateDefinition(
        string name,
        string description,
        DataSourceType sourceType,
        string sourceConfigurationJson,
        DataDestinationType destinationType,
        string destinationConfigurationJson,
        LoadStrategy loadStrategy)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("ETL job name is required.");
        }

        if (string.IsNullOrWhiteSpace(sourceConfigurationJson))
        {
            throw new DomainException("Source configuration is required.");
        }

        if (string.IsNullOrWhiteSpace(destinationConfigurationJson))
        {
            throw new DomainException("Destination configuration is required.");
        }

        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        SourceType = sourceType;
        SourceConfigurationJson = sourceConfigurationJson.Trim();
        DestinationType = destinationType;
        DestinationConfigurationJson = destinationConfigurationJson.Trim();
        LoadStrategy = loadStrategy;
        MarkUpdated();
    }

    public void ReplaceFieldMappings(IEnumerable<FieldMapping> mappings)
    {
        ArgumentNullException.ThrowIfNull(mappings);

        _fieldMappings.Clear();
        _fieldMappings.AddRange(mappings);
        MarkUpdated();
    }

    public EtlJobHistory MarkQueued()
    {
        CurrentStatus = EtlJobStatus.Queued;
        return AddHistoryEntry(EtlJobStatus.Queued);
    }

    public EtlJobHistory MarkRunning()
    {
        CurrentStatus = EtlJobStatus.Running;
        return AddHistoryEntry(EtlJobStatus.Running);
    }

    public EtlJobHistory MarkSucceeded(int recordsRead, int recordsTransformed, int recordsLoaded, int recordsFailed)
    {
        CurrentStatus = EtlJobStatus.Succeeded;
        return CompleteLatestExecution(EtlJobStatus.Succeeded, recordsRead, recordsTransformed, recordsLoaded, recordsFailed, null);
    }

    public EtlJobHistory MarkFailed(
        string errorMessage,
        int recordsRead = 0,
        int recordsTransformed = 0,
        int recordsLoaded = 0,
        int recordsFailed = 0)
    {
        if (string.IsNullOrWhiteSpace(errorMessage))
        {
            throw new DomainException("Failure message is required.");
        }

        CurrentStatus = EtlJobStatus.Failed;
        return CompleteLatestExecution(EtlJobStatus.Failed, recordsRead, recordsTransformed, recordsLoaded, recordsFailed, errorMessage.Trim());
    }

    public void Deactivate()
    {
        IsActive = false;
        MarkUpdated();
    }

    public void Activate()
    {
        IsActive = true;
        MarkUpdated();
    }

    private EtlJobHistory AddHistoryEntry(EtlJobStatus status)
    {
        var entry = EtlJobHistory.Started(Id, status);
        _jobHistory.Add(entry);
        MarkUpdated();
        return entry;
    }

    private EtlJobHistory CompleteLatestExecution(
        EtlJobStatus finalStatus,
        int recordsRead,
        int recordsTransformed,
        int recordsLoaded,
        int recordsFailed,
        string? errorMessage)
    {
        var runningEntry = _jobHistory
            .LastOrDefault(x => x.Status is EtlJobStatus.Queued or EtlJobStatus.Running && x.CompletedAtUtc is null)
            ?? AddHistoryEntry(EtlJobStatus.Running);

        runningEntry.Complete(finalStatus, recordsRead, recordsTransformed, recordsLoaded, recordsFailed, errorMessage);
        MarkUpdated();
        return runningEntry;
    }
}
