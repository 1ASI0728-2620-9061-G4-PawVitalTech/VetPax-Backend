
using VetPax.Infrastructure.Modules.AdherenceGamification.Processing;

namespace VetPax.Api.Modules.AdherenceGamification.BackgroundServices;

public sealed class AdherenceOutboxWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AdherenceOutboxWorker> _logger;

    public AdherenceOutboxWorker(
        IServiceScopeFactory scopes,
        IConfiguration configuration,
        ILogger<AdherenceOutboxWorker> logger)
    {
        _scopes = scopes;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        var enabled = _configuration.GetValue<bool>(
            "AdherenceGamification:OutboxWorker:Enabled");

        if (!enabled)
        {
            _logger.LogInformation(
                "Adherence outbox worker is disabled.");
            return;
        }

        using var timer = new PeriodicTimer(
            TimeSpan.FromSeconds(30));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopes.CreateScope();

                var processor = scope.ServiceProvider
                    .GetRequiredService<AdherenceOutboxProcessor>();

                var count = await processor.ProcessBatchAsync(
                    20,
                    stoppingToken);

                if (count > 0)
                {
                    _logger.LogInformation(
                        "Published {Count} adherence events.",
                        count);
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
                    "Adherence outbox processing failed.");
            }

            try
            {
                if (!await timer.WaitForNextTickAsync(
                        stoppingToken))
                {
                    break;
                }
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}
