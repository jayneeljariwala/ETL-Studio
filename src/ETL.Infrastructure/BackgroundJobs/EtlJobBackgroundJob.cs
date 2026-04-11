using ETL.Application.ETL.Abstractions;
using ETL.Application.ETL.Models;
using ETL.Domain.Enums;
using ETL.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ETL.Infrastructure.BackgroundJobs;

public sealed class EtlJobBackgroundJob
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IEtlEngine _etlEngine;
    private readonly ILogger<EtlJobBackgroundJob> _logger;

    public EtlJobBackgroundJob(
        ApplicationDbContext dbContext,
        IEtlEngine etlEngine,
        ILogger<EtlJobBackgroundJob> logger)
    {
        _dbContext = dbContext;
        _etlEngine = etlEngine;
        _logger = logger;
    }

    public async Task ExecuteAsync(Guid jobId)
    {
        var job = await _dbContext.EtlJobs
            .Include(x => x.FieldMappings)
            .Include(x => x.JobHistory)
            .FirstOrDefaultAsync(x => x.Id == jobId);

        if (job is null)
        {
            _logger.LogWarning("Background ETL job {JobId} was not found.", jobId);
            return;
        }

        if (!job.IsActive)
        {
            job.MarkFailed("ETL job is inactive and cannot be executed.");
            await _dbContext.SaveChangesAsync();
            return;
        }

        if (job.FieldMappings.Count == 0)
        {
            job.MarkFailed("No field mappings configured.");
            await _dbContext.SaveChangesAsync();
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

        var executionResult = await _etlEngine.ExecuteAsync(request, CancellationToken.None);
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

        await _dbContext.SaveChangesAsync();
    }
}
