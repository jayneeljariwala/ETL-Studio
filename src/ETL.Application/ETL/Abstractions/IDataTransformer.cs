using ETL.Application.ETL.Models;

namespace ETL.Application.ETL.Abstractions;

public interface IDataTransformer
{
    Task<RecordTransformResult> TransformAsync(
        IReadOnlyDictionary<string, object?> sourceRecord,
        IReadOnlyCollection<FieldMappingDefinition> mappings,
        CancellationToken cancellationToken);
}
