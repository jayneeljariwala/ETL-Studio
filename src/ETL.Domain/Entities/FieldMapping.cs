using ETL.Domain.Common;
using ETL.Domain.ValueObjects;

namespace ETL.Domain.Entities;

public sealed class FieldMapping : Entity
{
    private readonly List<TransformationStep> _transformationSteps = new();

    public Guid EtlJobId { get; private set; }
    public string SourceField { get; private set; } = default!;
    public string DestinationField { get; private set; } = default!;
    public int Order { get; private set; }
    public bool IsRequired { get; private set; }
    public string? DefaultValue { get; private set; }
    public EtlJob? EtlJob { get; private set; }

    public IReadOnlyCollection<TransformationStep> TransformationSteps => _transformationSteps
        .OrderBy(x => x.Order)
        .ToArray();

    private FieldMapping()
    {
    }

    public static FieldMapping Create(
        Guid etlJobId,
        string sourceField,
        string destinationField,
        int order,
        bool isRequired,
        string? defaultValue,
        IEnumerable<TransformationStep>? transformationSteps = null)
    {
        if (etlJobId == Guid.Empty)
        {
            throw new DomainException("ETL job id is required.");
        }

        if (string.IsNullOrWhiteSpace(sourceField))
        {
            throw new DomainException("Source field is required.");
        }

        if (string.IsNullOrWhiteSpace(destinationField))
        {
            throw new DomainException("Destination field is required.");
        }

        if (order < 0)
        {
            throw new DomainException("Field mapping order cannot be negative.");
        }

        var entity = new FieldMapping
        {
            Id = Guid.NewGuid(),
            EtlJobId = etlJobId,
            SourceField = sourceField.Trim(),
            DestinationField = destinationField.Trim(),
            Order = order,
            IsRequired = isRequired,
            DefaultValue = string.IsNullOrWhiteSpace(defaultValue) ? null : defaultValue.Trim()
        };

        if (transformationSteps is not null)
        {
            entity._transformationSteps.AddRange(transformationSteps.OrderBy(x => x.Order));
        }

        return entity;
    }

    public void Update(
        string sourceField,
        string destinationField,
        int order,
        bool isRequired,
        string? defaultValue,
        IEnumerable<TransformationStep>? transformationSteps = null)
    {
        if (string.IsNullOrWhiteSpace(sourceField))
        {
            throw new DomainException("Source field is required.");
        }

        if (string.IsNullOrWhiteSpace(destinationField))
        {
            throw new DomainException("Destination field is required.");
        }

        if (order < 0)
        {
            throw new DomainException("Field mapping order cannot be negative.");
        }

        SourceField = sourceField.Trim();
        DestinationField = destinationField.Trim();
        Order = order;
        IsRequired = isRequired;
        DefaultValue = string.IsNullOrWhiteSpace(defaultValue) ? null : defaultValue.Trim();

        _transformationSteps.Clear();
        if (transformationSteps is not null)
        {
            _transformationSteps.AddRange(transformationSteps.OrderBy(x => x.Order));
        }
    }
}
