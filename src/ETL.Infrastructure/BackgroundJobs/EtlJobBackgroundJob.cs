using ETL.Application.ETL.Abstractions;
using ETL.Application.ETL.Models;
using ETL.Application.Interfaces.Repositories;
using ETL.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace ETL.Infrastructure.BackgroundJobs;

public sealed class EtlJobBackgroundJob
{
    private readonly IEtlJobRepository _etlJobRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEtlEngine _etlEngine;
    private readonly ILogger<EtlJobBackgroundJob> _logger;

    public EtlJobBackgroundJob(
        IEtlJobRepository etlJobRepository,
        IUnitOfWork unitOfWork,
        IEtlEngine etlEngine,
        ILogger<EtlJobBackgroundJob> logger)
    {
        _etlJobRepository = etlJobRepository;
        _unitOfWork = unitOfWork;
        _etlEngine = etlEngine;
        _logger = logger;
    }

    public async Task ExecuteAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var job = await _etlJobRepository.GetJobForExecutionAsync(jobId, cancellationToken);

        if (job is null)
        {
            _logger.LogWarning("Background ETL job {JobId} was not found.", jobId);
            return;
        }

        if (!job.IsActive)
        {
            job.MarkFailed("ETL job is inactive and cannot be executed.");
            await _unitOfWork.SaveChangesAsync();
            return;
        }

        if (job.FieldMappings.Count == 0)
        {
            job.MarkFailed("No field mappings configured.");
            await _unitOfWork.SaveChangesAsync();
            return;
        }

        var request = new EtlExecutionRequest
        {
            EtlJobId = job.Id,
            SourceType = job.SourceType,
            SourceConfigurationJson = job.SourceConfigurationJson,
            DestinationType = job.DestinationType,
            DestinationConfigurationJson = job.DestinationConfigurationJson,
            LoadStrategy = job.LoadStrategy,
            Mappings = job.FieldMappings
                .OrderBy(x => x.Order)
                .Select(x => new FieldMappingDefinition
                {
                    SourceField = x.SourceField,
                    DestinationField = x.DestinationField,
                    IsRequired = x.IsRequired,
                    DefaultValue = x.DefaultValue,
                    Order = x.Order,
                    Transformations = x.TransformationSteps
                        .OrderBy(step => step.Order)
                        .Select(step => new TransformationDefinition
                        {
                            Type = step.Type,
                            Parameter = step.Parameter,
                            Order = step.Order
                        })
                        .ToList()
                })
                .ToList()
        };

        var executionResult = await _etlEngine.ExecuteAsync(request, cancellationToken);
        if (executionResult.IsSuccess)
        {
            job.MarkSucceeded(
                executionResult.RecordsRead,
                executionResult.RecordsTransformed,
                executionResult.RecordsLoaded,
                executionResult.RecordsFailed);
        }
        else
        {
            job.MarkFailed(
                executionResult.ErrorMessage ?? "ETL execution failed in background processing.",
                executionResult.RecordsRead,
                executionResult.RecordsTransformed,
                executionResult.RecordsLoaded,
                executionResult.RecordsFailed);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
