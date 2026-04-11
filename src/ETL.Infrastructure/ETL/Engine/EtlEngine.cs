using ETL.Application.ETL.Abstractions;
using ETL.Application.ETL.Models;
using ETL.Domain.Common;
using ETL.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace ETL.Infrastructure.ETL.Engine;

public sealed class EtlEngine : IEtlEngine
{
    private readonly IReadOnlyDictionary<DataSourceType, IDataExtractor> _extractors;
    private readonly IReadOnlyDictionary<DataDestinationType, IDataLoader> _loaders;
    private readonly IDataTransformer _transformer;
    private readonly ILogger<EtlEngine> _logger;

    public EtlEngine(
        IEnumerable<IDataExtractor> extractors,
        IEnumerable<IDataLoader> loaders,
        IDataTransformer transformer,
        ILogger<EtlEngine> logger)
    {
        _extractors = extractors.ToDictionary(x => x.SourceType);
        _loaders = loaders.ToDictionary(x => x.DestinationType);
        _transformer = transformer;
        _logger = logger;
    }

    public async Task<EtlExecutionResult> ExecuteAsync(EtlExecutionRequest request, CancellationToken cancellationToken)
    {
        if (!_extractors.TryGetValue(request.SourceType, out var extractor))
        {
            throw new DomainException($"No extractor found for source type '{request.SourceType}'.");
        }

        if (!_loaders.TryGetValue(request.DestinationType, out var loader))
        {
            throw new DomainException($"No loader found for destination type '{request.DestinationType}'.");
        }

        var result = new EtlExecutionResult();
        var batchSize = request.BatchSize > 0 ? request.BatchSize : 1000;
        var batch = new List<IReadOnlyDictionary<string, object?>>(batchSize);

        _logger.LogInformation("ETL job {JobId} started. Source: {SourceType}, Destination: {DestinationType}, Strategy: {LoadStrategy}",
            request.EtlJobId,
            request.SourceType,
            request.DestinationType,
            request.LoadStrategy);

        try
        {
            await foreach (var sourceRecord in extractor.ExtractAsync(request.SourceConfigurationJson, cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();
                result.RecordsRead++;

                var transformed = await _transformer.TransformAsync(sourceRecord, request.Mappings, cancellationToken);
                if (!transformed.IsValid)
                {
                    result.RecordsFailed++;
                    if (!string.IsNullOrWhiteSpace(transformed.Error) && result.ValidationErrors.Count < 100)
                    {
                        result.ValidationErrors.Add(transformed.Error);
                    }

                    _logger.LogWarning(
                        "ETL job {JobId} record validation failed at read index {RecordIndex}. Reason: {Reason}",
                        request.EtlJobId,
                        result.RecordsRead,
                        transformed.Error);
                    continue;
                }

                result.RecordsTransformed++;
                batch.Add(transformed.Record);

                if (batch.Count >= batchSize)
                {
                    result.RecordsLoaded += await loader.LoadAsync(
                        request.DestinationConfigurationJson,
                        request.LoadStrategy,
                        batch,
                        cancellationToken);

                    _logger.LogInformation(
                        "ETL job {JobId} batch loaded. BatchSize: {BatchSize}, TotalLoaded: {TotalLoaded}",
                        request.EtlJobId,
                        batch.Count,
                        result.RecordsLoaded);
                    batch.Clear();
                }
            }

            if (batch.Count > 0)
            {
                result.RecordsLoaded += await loader.LoadAsync(
                    request.DestinationConfigurationJson,
                    request.LoadStrategy,
                    batch,
                    cancellationToken);

                _logger.LogInformation(
                    "ETL job {JobId} final batch loaded. BatchSize: {BatchSize}, TotalLoaded: {TotalLoaded}",
                    request.EtlJobId,
                    batch.Count,
                    result.RecordsLoaded);
            }

            result.IsSuccess = true;
            _logger.LogInformation(
                "ETL job {JobId} completed successfully. Read: {Read}, Transformed: {Transformed}, Loaded: {Loaded}, Failed: {Failed}",
                request.EtlJobId,
                result.RecordsRead,
                result.RecordsTransformed,
                result.RecordsLoaded,
                result.RecordsFailed);
        }
        catch (Exception ex)
        {
            result.IsSuccess = false;
            result.ErrorMessage = ex.Message;
            _logger.LogError(ex, "ETL job {JobId} failed after processing {Read} records.", request.EtlJobId, result.RecordsRead);
        }

        return result;
    }
}
