using ETL.Domain.Common;
using ETL.Domain.Entities;
using ETL.Domain.Enums;
using ETL.Domain.ValueObjects;
using ETL.Infrastructure.BackgroundJobs;
using ETL.Infrastructure.Identity;
using ETL.Application.Interfaces.Repositories;
using ETL.Web.Models.EtlJobs;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ETL.Web.Controllers;

[Authorize]
public sealed class EtlJobsController : Controller
{
    private readonly IEtlJobRepository _etlJobRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly UserManager<AppIdentityUser> _userManager;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly ILogger<EtlJobsController> _logger;

    public EtlJobsController(
        IEtlJobRepository etlJobRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        UserManager<AppIdentityUser> userManager,
        IBackgroundJobClient backgroundJobClient,
        ILogger<EtlJobsController> logger)
    {
        _etlJobRepository = etlJobRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _userManager = userManager;
        _backgroundJobClient = backgroundJobClient;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var allJobs = await _etlJobRepository.GetAllJobsAsync(cancellationToken);
        var jobs = allJobs
            .Select(x => new EtlJobListItemViewModel
            {
                Id = x.Id,
                Name = x.Name,
                SourceType = x.SourceType.ToString(),
                DestinationType = x.DestinationType.ToString(),
                CurrentStatus = x.CurrentStatus.ToString(),
                IsActive = x.IsActive,
                CreatedAtUtc = x.CreatedAtUtc
            })
            .ToList();

        return View(jobs);
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var job = await _etlJobRepository.GetJobWithDetailsByIdAsync(id, false, cancellationToken);

        if (job is null)
        {
            return NotFound();
        }

        var model = new EtlJobDetailsViewModel
        {
            Id = job.Id,
            Name = job.Name,
            Description = job.Description,
            SourceType = job.SourceType.ToString(),
            DestinationType = job.DestinationType.ToString(),
            LoadStrategy = job.LoadStrategy.ToString(),
            CurrentStatus = job.CurrentStatus.ToString(),
            IsActive = job.IsActive,
            SourceConfigurationJson = job.SourceConfigurationJson,
            DestinationConfigurationJson = job.DestinationConfigurationJson,
            FieldMappings = job.FieldMappings
                .OrderBy(x => x.Order)
                .Select(x => new FieldMappingInputViewModel
                {
                    SourceField = x.SourceField,
                    DestinationField = x.DestinationField,
                    Order = x.Order,
                    IsRequired = x.IsRequired,
                    DefaultValue = x.DefaultValue,
                    Transformations = x.TransformationSteps
                        .Select(step => new TransformationInputViewModel
                        {
                            Type = step.Type,
                            Parameter = step.Parameter,
                            Order = step.Order
                        })
                        .ToList()
                })
                .ToList()
        };

        return View(model);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new EtlJobUpsertViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EtlJobUpsertViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var ownerId = await EnsureApplicationUserAsync(cancellationToken);
            var job = EtlJob.Create(
                model.Name,
                model.Description,
                model.SourceType,
                model.SourceConfigurationJson,
                model.DestinationType,
                model.DestinationConfigurationJson,
                model.LoadStrategy,
                ownerId);

            var mappings = BuildDomainMappings(job.Id, model.FieldMappings);
            job.ReplaceFieldMappings(mappings);

            _etlJobRepository.AddJob(job);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            TempData["SuccessMessage"] = "ETL job created successfully.";
            return RedirectToAction(nameof(Details), new { id = job.Id });
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var job = await _etlJobRepository.GetJobWithDetailsByIdAsync(id, false, cancellationToken);

        if (job is null)
        {
            return NotFound();
        }

        var model = new EtlJobUpsertViewModel
        {
            Id = job.Id,
            Name = job.Name,
            Description = job.Description,
            SourceType = job.SourceType,
            DestinationType = job.DestinationType,
            LoadStrategy = job.LoadStrategy,
            SourceConfigurationJson = job.SourceConfigurationJson,
            DestinationConfigurationJson = job.DestinationConfigurationJson,
            IsActive = job.IsActive,
            FieldMappings = job.FieldMappings
                .OrderBy(x => x.Order)
                .Select(x => new FieldMappingInputViewModel
                {
                    SourceField = x.SourceField,
                    DestinationField = x.DestinationField,
                    Order = x.Order,
                    IsRequired = x.IsRequired,
                    DefaultValue = x.DefaultValue,
                    Transformations = x.TransformationSteps
                        .OrderBy(step => step.Order)
                        .Select(step => new TransformationInputViewModel
                        {
                            Type = step.Type,
                            Parameter = step.Parameter,
                            Order = step.Order
                        })
                        .ToList()
                })
                .ToList()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, EtlJobUpsertViewModel model, CancellationToken cancellationToken)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var job = await _etlJobRepository.GetJobWithDetailsByIdAsync(id, true, cancellationToken);

        if (job is null)
        {
            return NotFound();
        }

        try
        {
            job.UpdateDefinition(
                model.Name,
                model.Description,
                model.SourceType,
                model.SourceConfigurationJson,
                model.DestinationType,
                model.DestinationConfigurationJson,
                model.LoadStrategy);

            var mappings = BuildDomainMappings(job.Id, model.FieldMappings);
            job.ReplaceFieldMappings(mappings);

            if (model.IsActive)
            {
                job.Activate();
            }
            else
            {
                job.Deactivate();
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            TempData["SuccessMessage"] = "ETL job updated successfully.";
            return RedirectToAction(nameof(Details), new { id = job.Id });
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Run(Guid id, CancellationToken cancellationToken)
    {
        var job = await _etlJobRepository.GetJobForExecutionAsync(id, cancellationToken);

        if (job is null)
        {
            return NotFound();
        }

        if (job.FieldMappings.Count == 0)
        {
            TempData["ErrorMessage"] = "Add field mappings before running this ETL job.";
            return RedirectToAction(nameof(Details), new { id });
        }

        if (job.CurrentStatus is EtlJobStatus.Queued or EtlJobStatus.Running)
        {
            TempData["ErrorMessage"] = "This ETL job is already queued or running.";
            return RedirectToAction(nameof(Details), new { id });
        }

        job.MarkQueued();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var backgroundJobId = _backgroundJobClient.Enqueue<EtlJobBackgroundJob>(x => x.ExecuteAsync(id, CancellationToken.None));
        _logger.LogInformation("Queued ETL job {JobId} to Hangfire background job {BackgroundJobId}.", id, backgroundJobId);
        TempData["SuccessMessage"] = $"ETL job queued successfully. Background Id: {backgroundJobId}";

        return RedirectToAction(nameof(History), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        var job = await _etlJobRepository.GetJobByIdAsync(id, true, cancellationToken);
        if (job is null)
        {
            return NotFound();
        }

        job.Activate();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        var job = await _etlJobRepository.GetJobByIdAsync(id, true, cancellationToken);
        if (job is null)
        {
            return NotFound();
        }

        job.Deactivate();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> History(Guid id, CancellationToken cancellationToken)
    {
        var job = await _etlJobRepository.GetJobByIdAsync(id, false, cancellationToken);

        if (job is null)
        {
            return NotFound();
        }

        var history = await _etlJobRepository.GetJobHistoryAsync(id, cancellationToken);
        var runs = history
            .Select(x => new EtlJobRunHistoryItemViewModel
            {
                HistoryId = x.Id,
                Status = x.Status.ToString(),
                StartedAtUtc = x.StartedAtUtc,
                CompletedAtUtc = x.CompletedAtUtc,
                RecordsRead = x.RecordsRead,
                RecordsTransformed = x.RecordsTransformed,
                RecordsLoaded = x.RecordsLoaded,
                RecordsFailed = x.RecordsFailed,
                ErrorMessage = x.ErrorMessage
            })
            .ToList();

        return View(new EtlJobRunHistoryViewModel
        {
            JobId = job.Id,
            JobName = job.Name,
            Runs = runs
        });
    }

    [HttpGet]
    public async Task<IActionResult> Logs(Guid id, CancellationToken cancellationToken)
    {
        var jobExists = await _etlJobRepository.JobExistsAsync(id, cancellationToken);

        if (!jobExists)
        {
            return NotFound();
        }

        var history = await _etlJobRepository.GetJobErrorHistoryAsync(id, cancellationToken);
        var errorRuns = history
            .Select(x => new EtlJobRunHistoryItemViewModel
            {
                HistoryId = x.Id,
                Status = x.Status.ToString(),
                StartedAtUtc = x.StartedAtUtc,
                CompletedAtUtc = x.CompletedAtUtc,
                RecordsRead = x.RecordsRead,
                RecordsTransformed = x.RecordsTransformed,
                RecordsLoaded = x.RecordsLoaded,
                RecordsFailed = x.RecordsFailed,
                ErrorMessage = x.ErrorMessage
            })
            .ToList();

        return View(new EtlJobRunHistoryViewModel
        {
            JobId = id,
            JobName = "Job Error Logs",
            Runs = errorRuns
        });
    }

    private async Task<Guid> EnsureApplicationUserAsync(CancellationToken cancellationToken)
    {
        var identityUser = await _userManager.GetUserAsync(User);
        if (identityUser is null)
        {
            throw new DomainException("You must be logged in to create an ETL job.");
        }

        var appUser = await _userRepository.GetUserByIdentityIdAsync(identityUser.Id, cancellationToken);

        if (appUser is not null)
        {
            return appUser.Id;
        }

        appUser = ApplicationUser.Create(
            identityUser.Id,
            identityUser.UserName ?? identityUser.Email ?? "etl-user",
            identityUser.Email ?? $"{identityUser.Id}@local");

        _userRepository.AddUser(appUser);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return appUser.Id;
    }

    private static IReadOnlyCollection<FieldMapping> BuildDomainMappings(
        Guid jobId,
        IReadOnlyCollection<FieldMappingInputViewModel> mappings)
    {
        return mappings
            .OrderBy(x => x.Order)
            .Select(mapping =>
                FieldMapping.Create(
                    jobId,
                    mapping.SourceField,
                    mapping.DestinationField,
                    mapping.Order,
                    mapping.IsRequired,
                    mapping.DefaultValue,
                    mapping.Transformations
                        .OrderBy(x => x.Order)
                        .Where(t => Enum.IsDefined(t.Type) && (int)t.Type > 0)
                        .Select(t => TransformationStep.Create(t.Type, t.Order, t.Parameter))))
            .ToList();
    }
}
