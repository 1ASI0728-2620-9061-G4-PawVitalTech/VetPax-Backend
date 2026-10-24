using System.Text.Json;
using VetPax.Api.Modules.MedicationTreatment.Controllers;
using VetPax.Infrastructure.Modules.MedicationTreatment.Notifications;
using Xunit;

namespace VetPax.Tests.Infrastructure.MedicationTreatment;

public sealed class MedicationFirebaseTests
{
    [Fact]
    public void TokenHash_IsDeterministic()
    {
        const string token = "sample-fcm-token-123";

        var first = MedicationDeviceTokenStore.HashToken(token);
        var second = MedicationDeviceTokenStore.HashToken(token);

        Assert.Equal(first, second);
        Assert.Equal(64, first.Length);
        Assert.NotEqual(token, first);
    }

    [Fact]
    public void DifferentTokens_HaveDifferentHashes()
    {
        var first = MedicationDeviceTokenStore.HashToken(
            "device-token-one");

        var second = MedicationDeviceTokenStore.HashToken(
            "device-token-two");

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void EmptyToken_IsRejected()
    {
        Assert.Throws<ArgumentException>(() =>
            MedicationDeviceTokenStore.HashToken(" "));
    }

    [Fact]
    public void Registration_RequiresToken()
    {
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<RegisterMedicationDeviceRequest>(
                """{"platform":"android"}"""));
    }

    [Fact]
    public void Registration_RequiresPlatform()
    {
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<RegisterMedicationDeviceRequest>(
                """{"token":"sample-token"}"""));
    }
}