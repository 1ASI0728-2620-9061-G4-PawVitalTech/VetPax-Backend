using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using VetPax.Application.Modules.IdentityAccessManagement.UseCases.AuthenticateUser;
using VetPax.Application.Modules.IdentityAccessManagement.UseCases.RegisterAccount;
using VetPax.Application.Modules.IdentityAccessManagement.UseCases.ValidatePermission;
using VetPax.Domain.Modules.IdentityAccessManagement.Services;

namespace VetPax.Api.Modules.IdentityAccessManagement;

public static class IamApiDependencyInjection
{
    public static IServiceCollection AddIamApi(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<RegisterAccountHandler>();
        services.AddScoped<AuthenticateUserHandler>();
        services.AddScoped<ValidatePermissionHandler>();

        services.AddSingleton<RoleAccessPolicy>();

        // Limit anonymous account registration requests.
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode =
                StatusCodes.Status429TooManyRequests;

            options.AddPolicy(
                "iam-registration",
                context =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey:
                        context.Connection.RemoteIpAddress?
                            .ToString() ?? "unknown",

                        factory: _ =>
                            new FixedWindowRateLimiterOptions
                            {
                                PermitLimit = 5,
                                Window = TimeSpan.FromMinutes(1),
                                QueueLimit = 0,
                                AutoReplenishment = true
                            }));
        });

        return services;
    }
}