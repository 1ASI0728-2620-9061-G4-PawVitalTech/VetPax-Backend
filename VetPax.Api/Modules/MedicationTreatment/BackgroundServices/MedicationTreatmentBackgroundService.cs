
using Microsoft.Extensions.Options;
using VetPax.Application.Modules.MedicationTreatment.Models;
using VetPax.Application.Modules.MedicationTreatment.UseCases.GenerateMedicationDoses;
using VetPax.Infrastructure.Modules.MedicationTreatment.Processing;

namespace VetPax.Api.Modules.MedicationTreatment.BackgroundServices;

public sealed class MedicationTreatmentBackgroundService
    : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeProvider _clock;
    private readonly MedicationWorkerOptions _options;
    private readonly ILogger<MedicationTreatmentBackgroundService> _logger;

    public MedicationTreatmentBackgroundService(
        IServiceScopeFactory scopeFactory,
        TimeProvider clock,
        IOptions<MedicationWorkerOptions> options,
        ILogger<MedicationTreatmentBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _clock = clock;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        var jobs = new List<Task>();

        if (_options.DoseGenerationEnabled)
        {
            jobs.Add(RunLoopAsync(
                "MedicationDoseGeneration",
                TimeSpan.FromMinutes(
                    _options.DoseGenerationIntervalMinutes),
                GenerateDosesAsync,
                stoppingToken));
        }

        if (_options.RemindersEnabled)
        {
            jobs.Add(RunLoopAsync(
                "MedicationReminders",
                TimeSpan.FromSeconds(
                    _options.ReminderIntervalSeconds),
                ProcessRemindersAsync,
                stoppingToken));
        }

        if (_options.OutboxEnabled)
        {
            jobs.Add(RunLoopAsync(
                "MedicationOutbox",
                TimeSpan.FromSeconds(
                    _options.OutboxIntervalSeconds),
                ProcessOutboxAsync,
                stoppingToken));
        }

        if (jobs.Count == 0)
        {
            _logger.LogInformation(
                "All MedicationTreatment background jobs are disabled.");

            return;
        }

        _logger.LogInformation(
            "MedicationTreatment background jobs started. " +
            "Generation={Generation}, Reminders={Reminders}, Outbox={Outbox}",
            _options.DoseGenerationEnabled,
            _options.RemindersEnabled,
            _options.OutboxEnabled);

        await Task.WhenAll(jobs);
    }

    private async Task RunLoopAsync(
        string jobName,
        TimeSpan interval,
        Func<IServiceProvider, CancellationToken, Task<int>> action,
        CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(interval, _clock);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Un scope nuevo por ejecución:
                // nunca se reutiliza el mismo DbContext
                // entre dos iteraciones.
                await using var scope =
                    _scopeFactory.CreateAsyncScope();

                var processed = await action(
                    scope.ServiceProvider,
                    stoppingToken);

                if (processed > 0)
                {
                    _logger.LogInformation(
                        "{JobName} processed {Count} items.",
                        jobName,
                        processed);
                }
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                // Un fallo de MySQL, FCM u otro servicio
                // no debe detener permanentemente el worker.
                _logger.LogError(
                    exception,
                    "{JobName} failed. It will retry on the next cycle.",
                    jobName);
            }

            try
            {
                if (!await timer.WaitForNextTickAsync(stoppingToken))
                    break;
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        _logger.LogInformation(
            "{JobName} stopped.",
            jobName);
    }

    private Task<int> GenerateDosesAsync(
        IServiceProvider services,
        CancellationToken cancellationToken)
    {
        var handler =
            services.GetRequiredService<GenerateMedicationDosesHandler>();

        var utcDate = DateOnly.FromDateTime(
            _clock.GetUtcNow().UtcDateTime);

        var throughDate = utcDate.AddDays(
            _options.HorizonDays);

        return handler.HandleAsync(
            new GenerateMedicationDosesCommand(
                throughDate,
                _options.BatchSize),
            cancellationToken);
    }

    private Task<int> ProcessRemindersAsync(
        IServiceProvider services,
        CancellationToken cancellationToken)
    {
        var processor =
            services.GetRequiredService<MedicationReminderProcessor>();

        return processor.ProcessBatchAsync(cancellationToken);
    }

    private Task<int> ProcessOutboxAsync(
        IServiceProvider services,
        CancellationToken cancellationToken)
    {
        var processor =
            services.GetRequiredService<MedicationOutboxProcessor>();

        return processor.ProcessBatchAsync(cancellationToken);
    }
}
