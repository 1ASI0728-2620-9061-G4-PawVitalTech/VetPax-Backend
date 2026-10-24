
using VetPax.Infrastructure.Modules.AppointmentManagement.Processing;

namespace VetPax.Api.HostedServices;

public sealed class AppointmentDeliveryHostedService
    : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AppointmentDeliveryHostedService> _logger;

    public AppointmentDeliveryHostedService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<AppointmentDeliveryHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        var enabled = _configuration.GetValue<bool>(
            "AppointmentProcessing:Enabled");

        if (!enabled)
        {
            _logger.LogInformation(
                "Appointment background processing is disabled.");
            return;
        }

        _logger.LogInformation(
            "Appointment background processing started.");

        // Una ejecución inicial al arrancar y luego
        // comprobaciones cada 15 segundos.
        await ProcessOnceAsync(stoppingToken);

        using var timer = new PeriodicTimer(
            TimeSpan.FromSeconds(15));

        while (await timer.WaitForNextTickAsync(
                   stoppingToken))
        {
            await ProcessOnceAsync(stoppingToken);
        }
    }

    private async Task ProcessOnceAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            await using var scope =
                _scopeFactory.CreateAsyncScope();

            var outbox = scope.ServiceProvider
                .GetRequiredService<AppointmentOutboxProcessor>();

            var reminders = scope.ServiceProvider
                .GetRequiredService<AppointmentReminderProcessor>();

            var published = await outbox.ProcessBatchAsync(
                cancellationToken);

            var sent = await reminders.ProcessBatchAsync(
                cancellationToken);

            if (published > 0 || sent > 0)
            {
                _logger.LogInformation(
                    "Appointment processing: {Published} events, " +
                    "{Sent} reminders.",
                    published,
                    sent);
            }
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            // Apagado normal.
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Appointment background processing cycle failed.");
        }
    }
}
