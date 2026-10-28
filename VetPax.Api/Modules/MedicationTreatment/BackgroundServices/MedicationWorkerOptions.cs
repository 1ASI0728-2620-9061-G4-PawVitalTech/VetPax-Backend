namespace VetPax.Api.Modules.MedicationTreatment.BackgroundServices;

public sealed class MedicationWorkerOptions
{
    public const string SectionName =
        "MedicationTreatment:Workers";

    // Se activa al iniciar la API.
    public bool DoseGenerationEnabled { get; set; } = true;

    // Se activará al terminar FCM (TS06).
    public bool RemindersEnabled { get; set; } = false;

    // Se activará cuando el publicador de eventos
    // esté conectado y probado.
    public bool OutboxEnabled { get; set; } = false;

    // Frecuencia de ejecución.
    public int DoseGenerationIntervalMinutes { get; set; } = 30;
    public int ReminderIntervalSeconds { get; set; } = 60;
    public int OutboxIntervalSeconds { get; set; } = 30;

    // Generar las dosis de los próximos tres días.
    public int HorizonDays { get; set; } = 3;

    // Máximo número de planes por ejecución.
    public int BatchSize { get; set; } = 20;

    public bool IsValid()
    {
        return DoseGenerationIntervalMinutes is >= 1 and <= 1440
               && ReminderIntervalSeconds is >= 10 and <= 3600
               && OutboxIntervalSeconds is >= 10 and <= 3600
               && HorizonDays is >= 0 and <= 7
               && BatchSize is >= 1 and <= 100;
    }
}