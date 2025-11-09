using AutoInsight.Auth;
using AutoInsight.Yards;
using AutoInsight.Vehicles;
using AutoInsight.EmployeeInvites;
using AutoInsight.YardEmployees;
using AutoInsight.Data;
using AutoInsight.ML;
using AutoInsight.Services;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using AutoInsight.Models;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi("v2");

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

void ConfigureDbContext(DbContextOptionsBuilder options)
    => options.UseNpgsql(
            connectionString,
            o =>
            {
                o.MapEnum<VehicleModel>("vehicle_model");
                o.MapEnum<EmployeeRole>("employee_role");
                o.MapEnum<InviteStatus>("invite_status");
                o.MapEnum<VehicleStatus>("vehicle_status");
            })
        .UseSnakeCaseNamingConvention();

builder.Services.AddDbContext<AppDbContext>(ConfigureDbContext);
builder.Services.AddDbContextFactory<AppDbContext>(lifetime: ServiceLifetime.Scoped);

builder.Services.AddScoped<IYardCapacitySnapshotService, YardCapacitySnapshotService>();
builder.Services.AddScoped<IYardCapacityForecastService, YardCapacityForecastService>();
builder.Services
    .AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("database");

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.AddDocument("v2", "API v2", "/openapi/v2.json", isDefault: true);
    });
}

app.UseAuthenticatedUserExtraction();

app.MapGroup("/v2")
    .MapYardEnpoints()
    .MapVehicleEnpoints()
    .MapYardEmployeeEnpoints()
    .MapEmployeeInviteEnpoints();

app.MapHealthChecks("/health");

app.Run();

public partial class Program;
