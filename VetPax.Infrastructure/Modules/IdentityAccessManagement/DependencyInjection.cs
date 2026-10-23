
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using VetPax.Application.Modules.IdentityAccessManagement.Ports;
using VetPax.Infrastructure.Modules.IdentityAccessManagement.Adapters;
using VetPax.Infrastructure.Modules.IdentityAccessManagement.Configuration;

namespace VetPax.Infrastructure.Modules.IdentityAccessManagement;

public static class DependencyInjection
{
    public static IServiceCollection AddIamInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var section = configuration.GetSection("Keycloak");
        var settings = section.Get<KeycloakOptions>()
            ?? throw new InvalidOperationException(
                "Keycloak configuration is missing.");

        if (string.IsNullOrWhiteSpace(settings.BaseUrl) ||
            string.IsNullOrWhiteSpace(settings.Realm) ||
            string.IsNullOrWhiteSpace(settings.Audience) ||
            string.IsNullOrWhiteSpace(settings.AdminClientId) ||
            string.IsNullOrWhiteSpace(settings.AdminClientSecret))
        {
            throw new InvalidOperationException(
                "Incomplete Keycloak configuration.");
        }

        if (!environment.IsDevelopment() &&
            !settings.BaseUrl.StartsWith(
                "https://", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Keycloak requires HTTPS outside Development.");
        }

        services.Configure<KeycloakOptions>(section);
        services.AddHttpContextAccessor();

        services.AddHttpClient<
            IIdentityProvisioningPort,
            KeycloakIdentityProvisioningAdapter>();

        services.AddScoped<
            IAuthenticatedUserPort,
            KeycloakAuthenticatedUserAdapter>();

        services.AddAuthentication(
            JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = settings.Authority;
                options.Audience = settings.Audience;
                options.RequireHttpsMetadata =
                    !environment.IsDevelopment();

                options.MapInboundClaims = false;

                options.TokenValidationParameters =
                    new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        RoleClaimType = ClaimTypes.Role
                    };

                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        var raw = context.Principal?
                            .FindFirst("realm_access")?.Value;

                        if (string.IsNullOrEmpty(raw))
                            return Task.CompletedTask;

                        try
                        {
                            using var json = JsonDocument.Parse(raw);

                            if (!json.RootElement.TryGetProperty(
                                "roles", out var roles) ||
                                roles.ValueKind != JsonValueKind.Array)
                                return Task.CompletedTask;

                            var identity =
                                context.Principal!.Identity
                                    as ClaimsIdentity;

                            foreach (var role in roles.EnumerateArray())
                            {
                                if (role.ValueKind != JsonValueKind.String)
                                    continue;

                                var name = role.GetString();

                                if (name is "owner" or "veterinarian"
                                    or "clinic_administrator")
                                {
                                    identity?.AddClaim(new Claim(
                                        ClaimTypes.Role, name));
                                }
                            }
                        }
                        catch (JsonException)
                        {
                            context.Fail("Invalid role claims.");
                        }

                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization();

        return services;
    }
}
