
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using VetPax.Infrastructure.Persistence;

namespace VetPax.Api.Persistence;

/// <summary>
/// Creates VetPaxDbContext for EF Core CLI commands
/// without starting the ASP.NET Core application.
/// </summary>
public sealed class VetPaxDesignTimeDbContextFactory
    : IDesignTimeDbContextFactory<VetPaxDbContext>
{
    public VetPaxDbContext CreateDbContext(string[] args)
    {
        var apiDirectory = FindApiDirectory();

        var environment =
            Environment.GetEnvironmentVariable(
                "ASPNETCORE_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable(
                "DOTNET_ENVIRONMENT")
            ?? "Development";

        var configuration = new ConfigurationBuilder()
            .SetBasePath(apiDirectory)
            .AddJsonFile(
                "appsettings.json",
                optional: true)
            .AddJsonFile(
                $"appsettings.{environment}.json",
                optional: true)
            .AddUserSecrets<ApiAssemblyMarker>(
                optional: true)
            .AddEnvironmentVariables()
            .Build();

        // Optional explicit connection for EF CLI.
        // Takes priority over user secrets.
        var connectionString =
            Environment.GetEnvironmentVariable(
                "VETPAX_EF_CONNECTION");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString =
                configuration.GetConnectionString("VetPaxDb");
        }

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "EF Core design-time connection is missing. " +
                "Configure ConnectionStrings:VetPaxDb in " +
                "VetPax.Api user secrets or set " +
                "VETPAX_EF_CONNECTION.");
        }

        var options = new DbContextOptionsBuilder<VetPaxDbContext>()
            .UseMySql(
                connectionString,
                new MySqlServerVersion(
                    new Version(8, 0, 0)),
                mysql =>
                    mysql.MigrationsAssembly(
                        typeof(VetPaxDbContext)
                            .Assembly
                            .GetName()
                            .Name))
            .Options;

        return new VetPaxDbContext(options);
    }

    private static string FindApiDirectory()
    {
        var currentDirectory =
            Directory.GetCurrentDirectory();

        var candidates = new[]
        {
            currentDirectory,

            Path.Combine(
                currentDirectory,
                "VetPax.Api"),

            Path.Combine(
                currentDirectory,
                "src",
                "VetPax.Api"),

            Path.Combine(
                currentDirectory,
                "..",
                "VetPax.Api")
        };

        foreach (var candidate in candidates)
        {
            var fullPath = Path.GetFullPath(candidate);

            if (File.Exists(
                    Path.Combine(
                        fullPath,
                        "VetPax.Api.csproj")))
            {
                return fullPath;
            }
        }

        throw new DirectoryNotFoundException(
            "VetPax.Api project directory was not found.");
    }
}
