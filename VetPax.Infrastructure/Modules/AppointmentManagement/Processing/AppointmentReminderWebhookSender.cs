
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using VetPax.Infrastructure.Modules.AppointmentManagement.Persistence;

namespace VetPax.Infrastructure.Modules.AppointmentManagement.Processing;

public sealed class AppointmentReminderWebhookSender
    : IAppointmentReminderSender
{
    private readonly HttpClient _http;
    private readonly IConfiguration _configuration;

    public AppointmentReminderWebhookSender(
        HttpClient http,
        IConfiguration configuration)
    {
        _http = http;
        _configuration = configuration;
    }

    public async Task SendAsync(
        AppointmentRow appointment,
        CancellationToken cancellationToken = default)
    {
        var url = _configuration[
            "AppointmentProcessing:ReminderWebhookUrl"];

        var token = _configuration[
            "AppointmentProcessing:ReminderWebhookToken"];

        if (!Uri.TryCreate(
                url,
                UriKind.Absolute,
                out var endpoint) ||
            endpoint.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidOperationException(
                "Configure an HTTPS reminder webhook endpoint.");
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException(
                "Configure AppointmentProcessing:ReminderWebhookToken.");
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            endpoint);

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                token);

        // Clave estable para el mismo estado de la cita.
        // El receptor debe utilizarla para deduplicación.
        var deliveryKey =
            $"{appointment.AppointmentId:N}-{appointment.UpdatedAt.Ticks}";

        request.Headers.TryAddWithoutValidation(
            "Idempotency-Key",
            deliveryKey);

        request.Content = JsonContent.Create(new
        {
            notificationType = "appointment_reminder",
            appointmentId = appointment.AppointmentId,
            ownerId = appointment.OwnerId,
            petId = appointment.PetId,
            clinicId = appointment.ClinicId,
            veterinarianId = appointment.VeterinarianId,
            date = appointment.AppointmentDate,
            startTime = appointment.StartTime,
            reason = appointment.Reason,
            idempotencyKey = deliveryKey
        });

        using var response = await _http.SendAsync(
            request,
            cancellationToken);

        response.EnsureSuccessStatusCode();
    }
}
