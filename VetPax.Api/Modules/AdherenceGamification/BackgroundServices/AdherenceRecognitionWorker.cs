
using VetPax.Infrastructure.Modules.AdherenceGamification.Notifications;

namespace VetPax.Api.Modules.AdherenceGamification.BackgroundServices;

public sealed class AdherenceRecognitionWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AdherenceRecognitionWorker> _logger;

    public AdherenceRecognitionWorker(
        IServiceScopeFactory scopes,
        IConfiguration configuration,
        ILogger<AdherenceRecognitionWorker> logger)
    {
        _scopes = scopes;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        if (!_configuration.GetValue<bool>(
                "AdherenceGamification:RecognitionWorker:Enabled"))
        {
            _logger.LogInformation(
                "Adherence recognition worker is disabled.");
            return;
        }

        using var timer = new PeriodicTimer(
            TimeSpan.FromSeconds(60));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopes.CreateScope();

                var processor = scope.ServiceProvider
                    .GetRequiredService<RecognitionNotificationProcessor>();

                await processor.ProcessBatchAsync(
                    20,
                    stoppingToken);
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
                    "Adherence recognition processing failed.");
            }

            try
            {
                if (!await timer.WaitForNextTickAsync(
                        stoppingToken))
                    break;
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}
