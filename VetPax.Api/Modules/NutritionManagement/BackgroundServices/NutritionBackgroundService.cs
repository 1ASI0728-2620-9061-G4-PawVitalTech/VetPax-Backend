
using Microsoft.Extensions.Options;
using VetPax.Application.Modules.NutritionManagement.Models;
using VetPax.Application.Modules.NutritionManagement.UseCases;
using VetPax.Infrastructure.Modules.NutritionManagement.Notifications;

namespace VetPax.Api.Modules.NutritionManagement.BackgroundServices;

public sealed class NutritionBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly NutritionWorkerOptions _options;
    private readonly TimeProvider _clock;
    private readonly ILogger<NutritionBackgroundService> _logger;

    public NutritionBackgroundService(
        IServiceScopeFactory scopes,
        IOptions<NutritionWorkerOptions> options,
        TimeProvider clock,
        ILogger<NutritionBackgroundService> logger)
    {
        _scopes = scopes;
        _options = options.Value;
        _clock = clock;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation(
                "NutritionManagement worker is disabled.");

            return;
        }

        DateTime? lastGenerationAtUtc = null;

        while (!stoppingToken.IsCancellationRequested)
        {
            var now = _clock.GetUtcNow().UtcDateTime;

            var shouldGenerate =
                _options.GenerationEnabled &&
                (
                    !lastGenerationAtUtc.HasValue ||
                    now - lastGenerationAtUtc.Value >=
                    TimeSpan.FromMinutes(
                        _options.GenerateIntervalMinutes)
                );

            if (shouldGenerate)
            {
                try
                {
                    using var scope = _scopes.CreateScope();

                    var handler = scope.ServiceProvider
                        .GetRequiredService<GenerateFeedingRemindersHandler>();

                    var utcToday = DateOnly.FromDateTime(now);

                    // Cover time zones on either side of UTC.
                    var fromDate = utcToday.AddDays(-1);
                    var throughDate = utcToday.AddDays(
                        _options.DaysAhead + 1);

                    var generated = await handler.HandleAsync(
                        new GenerateFeedingRemindersCommand(
                            fromDate,
                            throughDate,
                            _options.BatchSize),
                        stoppingToken);

                    lastGenerationAtUtc = now;

                    _logger.LogInformation(
                        "Nutrition reminders generated: {Count}",
                        generated);
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Nutrition reminder generation failed.");
                }
            }

            if (_options.DeliveryEnabled)
            {
                try
                {
                    using var scope = _scopes.CreateScope();

                    var processor = scope.ServiceProvider
                        .GetRequiredService<NutritionReminderProcessor>();

                    var processed = await processor.ProcessBatchAsync(
                        _options.BatchSize,
                        stoppingToken);

                    if (processed > 0)
                    {
                        _logger.LogInformation(
                            "Nutrition reminders processed: {Count}",
                            processed);
                    }
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Nutrition reminder delivery failed.");
                }
            }

            try
            {
                await Task.Delay(
                    TimeSpan.FromSeconds(
                        _options.PollIntervalSeconds),
                    stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}
