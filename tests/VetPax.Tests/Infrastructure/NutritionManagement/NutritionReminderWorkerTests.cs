
using VetPax.Api.Modules.NutritionManagement.BackgroundServices;
using VetPax.Infrastructure.Modules.NutritionManagement.Notifications;
using Xunit;

namespace VetPax.Tests.Infrastructure.NutritionManagement;

public sealed class NutritionReminderWorkerTests
{
    private static readonly DateTime FailedAtUtc =
        new(2026, 10, 23, 20, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(1, 2)]
    [InlineData(2, 4)]
    [InlineData(3, 8)]
    [InlineData(4, 16)]
    public void RetryPolicy_UsesExponentialBackoff(
        int attempts,
        int expectedMinutes)
    {
        var next = NutritionReminderRetryPolicy.NextAttempt(
            attempts,
            FailedAtUtc);

        Assert.Equal(
            FailedAtUtc.AddMinutes(expectedMinutes),
            next);
    }

    [Fact]
    public void RetryPolicy_StopsAfterFiveAttempts()
    {
        var next = NutritionReminderRetryPolicy.NextAttempt(
            5,
            FailedAtUtc);

        Assert.Null(next);
    }

    [Fact]
    public void RetryPolicy_RejectsInvalidAttemptCount()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            NutritionReminderRetryPolicy.NextAttempt(
                0,
                FailedAtUtc));
    }

    [Fact]
    public void Worker_IsDisabledByDefault()
    {
        var options = new NutritionWorkerOptions();

        Assert.False(options.Enabled);
        Assert.False(options.GenerationEnabled);
        Assert.False(options.DeliveryEnabled);
        Assert.True(options.IsValid());
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(6)]
    public void Worker_RejectsInvalidGenerationHorizon(int days)
    {
        var options = new NutritionWorkerOptions
        {
            DaysAhead = days
        };

        Assert.False(options.IsValid());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Worker_RejectsInvalidBatchSize(int batchSize)
    {
        var options = new NutritionWorkerOptions
        {
            BatchSize = batchSize
        };

        Assert.False(options.IsValid());
    }

    [Fact]
    public void Worker_AllowsValidConfiguration()
    {
        var options = new NutritionWorkerOptions
        {
            Enabled = true,
            GenerationEnabled = true,
            DeliveryEnabled = false,
            PollIntervalSeconds = 60,
            GenerateIntervalMinutes = 30,
            DaysAhead = 3,
            BatchSize = 20
        };

        Assert.True(options.IsValid());
    }
}
