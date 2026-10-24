
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using VetPax.Api.Modules.MedicationTreatment.BackgroundServices;
using Xunit;

namespace VetPax.Tests.Api.MedicationTreatment;

public sealed class MedicationBackgroundServicesTests
{
    [Fact]
    public void DefaultOptions_EnableOnlyDoseGeneration()
    {
        var options = new MedicationWorkerOptions();

        Assert.True(options.DoseGenerationEnabled);
        Assert.False(options.RemindersEnabled);
        Assert.False(options.OutboxEnabled);

        Assert.Equal(3, options.HorizonDays);
        Assert.Equal(20, options.BatchSize);
        Assert.True(options.IsValid());
    }


    [Theory]
    [InlineData(-1, 20)]
    [InlineData(8, 20)]
    [InlineData(3, 0)]
    [InlineData(3, 101)]
    public void InvalidGenerationConfiguration_IsRejected(
        int horizonDays,
        int batchSize)
    {
        var options = new MedicationWorkerOptions
        {
            HorizonDays = horizonDays,
            BatchSize = batchSize
        };

        Assert.False(options.IsValid());
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(3, 20)]
    [InlineData(7, 100)]
    public void ValidGenerationConfiguration_IsAccepted(
        int horizonDays,
        int batchSize)
    {
        var options = new MedicationWorkerOptions
        {
            HorizonDays = horizonDays,
            BatchSize = batchSize
        };

        Assert.True(options.IsValid());
    }


    [Fact]
    public void Configuration_BindsWorkerOptions()
    {
        var values = new Dictionary<string, string?>
        {
            ["MedicationTreatment:Workers:DoseGenerationEnabled"] =
                "true",

            ["MedicationTreatment:Workers:RemindersEnabled"] =
                "false",

            ["MedicationTreatment:Workers:OutboxEnabled"] =
                "false",

            ["MedicationTreatment:Workers:HorizonDays"] =
                "5",

            ["MedicationTreatment:Workers:BatchSize"] =
                "10"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();

        var services = new ServiceCollection();

        services.AddMedicationBackgroundServices(configuration);

        using var provider = services.BuildServiceProvider();

        var options = provider
            .GetRequiredService<IOptions<MedicationWorkerOptions>>()
            .Value;

        Assert.True(options.IsValid());
        Assert.Equal(5, options.HorizonDays);
        Assert.Equal(10, options.BatchSize);
        Assert.False(options.RemindersEnabled);
    }

    [Fact]
    public void Configuration_InvalidValues_ThrowOptionsValidation()
    {
        var values = new Dictionary<string, string?>
        {
            ["MedicationTreatment:Workers:HorizonDays"] = "10"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();

        var services = new ServiceCollection();

        services.AddMedicationBackgroundServices(configuration);

        using var provider = services.BuildServiceProvider();

        Assert.Throws<OptionsValidationException>(() =>
        {
            _ = provider.GetRequiredService<
                IOptions<MedicationWorkerOptions>>().Value;
        });
    }

    [Fact]
    public async Task DisabledWorkers_DoNotRequireDatabaseOrFirebase()
    {
        var values = new Dictionary<string, string?>
        {
            ["MedicationTreatment:Workers:DoseGenerationEnabled"] =
                "false",

            ["MedicationTreatment:Workers:RemindersEnabled"] =
                "false",

            ["MedicationTreatment:Workers:OutboxEnabled"] =
                "false"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();

        var services = new ServiceCollection();

        services.AddLogging();
        services.AddSingleton<TimeProvider>(TimeProvider.System);
        services.AddMedicationBackgroundServices(configuration);

        await using var provider =
            services.BuildServiceProvider();

        var hostedService = provider
            .GetServices<IHostedService>()
            .OfType<MedicationTreatmentBackgroundService>()
            .Single();

        await hostedService.StartAsync(
            CancellationToken.None);

        await hostedService.StopAsync(
            CancellationToken.None);
    }
}
