
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Xunit;

namespace VetPax.Tests.Infrastructure.MedicationTreatment;

public sealed class MedicationFirebaseConnectivityTests
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task Firebase_DryRun_ValidatesBackendConnection()
    {
        // Arrange: project configured in Firebase.
        var projectId = Environment.GetEnvironmentVariable(
            "VETPAX_FIREBASE_PROJECT_ID");

        Assert.False(
            string.IsNullOrWhiteSpace(projectId),
            "VETPAX_FIREBASE_PROJECT_ID is required.");

        var credentialsPath = Environment.GetEnvironmentVariable(
            "GOOGLE_APPLICATION_CREDENTIALS");

        Assert.True(
            !string.IsNullOrWhiteSpace(credentialsPath) &&
            File.Exists(credentialsPath),
            "Firebase service account credentials were not found.");

        // Use a unique app to avoid interfering with
        // other Firebase tests or the API.
        var app = FirebaseApp.Create(
            new AppOptions
            {
                Credential =
                    GoogleCredential.GetApplicationDefault(),

                ProjectId = projectId
            },
            $"vetpax-fcm-test-{Guid.NewGuid():N}");

        try
        {
            var messaging = FirebaseMessaging.GetMessaging(app);

            // A dry run to a topic does not require
            // a registered phone or browser.
            var message = new Message
            {
                Topic = "vetpax_backend_connectivity_test",

                Data = new Dictionary<string, string>
                {
                    ["type"] = "connectivity_check",
                    ["source"] = "vetpax_backend"
                }
            };

            // Act: calls Google's real FCM API,
            // without delivering a notification.
            var response = await messaging.SendAsync(
                message,
                dryRun: true,
                cancellationToken: CancellationToken.None);

            // Assert
            Assert.False(string.IsNullOrWhiteSpace(response));
        }
        finally
        {
            app.Delete();
        }
    }
}
