using Microsoft.EntityFrameworkCore;
using VetPax.Application.Common.Ports;
using VetPax.Application.Modules.PetClinicalCare.UseCases.RegisterPet;
using VetPax.Domain.Modules.PetClinicalCare.Repositories;
using VetPax.Infrastructure.Modules.PetClinicalCare.Repositories;
using VetPax.Infrastructure.Persistence;
using System.Text.Json.Serialization;
using VetPax.Api;
using VetPax.Application.Modules.PetClinicalCare.UseCases.GetPet;
using VetPax.Application.Modules.PetClinicalCare.UseCases.GetClinicalHistory;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter()
        );
    });

builder.Services.AddOpenApi();

var connectionString = builder.Configuration
                           .GetConnectionString("VetPaxDb")
                       ?? throw new InvalidOperationException(
                           "VetPax database connection is not configured."
                       );

// Configure MySQL
builder.Services.AddDbContext<VetPaxDbContext>(options =>
    options.UseMySql(
        connectionString,
        new MySqlServerVersion(new Version(8, 0, 0))
    )
);

// Register repositories
builder.Services.AddScoped<IPetRepository, PetRepository>();
builder.Services.AddScoped<
    IClinicalRecordRepository,
    ClinicalRecordRepository>();

// Register application services
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<RegisterPetHandler>();
builder.Services.AddScoped<GetPetHandler>();
builder.Services.AddScoped<GetClinicalHistoryHandler>();
builder.Services.AddPetClinicalCare();

var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint(
            "/openapi/v1.json",
            "VetPax API v1"
        );
    });
    using var scope = app.Services.CreateScope();

    var dbContext = scope.ServiceProvider
        .GetRequiredService<VetPaxDbContext>();

    await dbContext.Database.MigrateAsync();
    // Keep your existing database migration code here
    // if it is already configured.
}


app.UseHttpsRedirection();
app.MapControllers();

app.Run();

public partial class Program { }