namespace VetPax.Infrastructure.Modules.NutritionManagement.Notifications;

public static class NutritionReminderRetryPolicy
{
    public const int MaxAttempts = 5;

    public static DateTime? NextAttempt(
        int attemptCount,
        DateTime failedAtUtc)
    {
        if (attemptCount < 1)
            throw new ArgumentOutOfRangeException(
                nameof(attemptCount));

        if (failedAtUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException(
                "The timestamp must be UTC.",
                nameof(failedAtUtc));

        if (attemptCount >= MaxAttempts)
            return null;

        var delayMinutes = Math.Min(
            60,
            Math.Pow(2, attemptCount));

        return failedAtUtc.AddMinutes(delayMinutes);
    }
}