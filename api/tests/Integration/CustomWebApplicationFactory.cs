using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoInsight.Data;
using AutoInsight.ML;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace AutoInsight.Tests.Integration;

internal sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"IntegrationTests_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseContentRoot(Directory.GetCurrentDirectory());

        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(DbContextOptions<AppDbContext>));
            services.RemoveAll(typeof(IDbContextFactory<AppDbContext>));
            services.RemoveAll(typeof(AppDbContext));
            services.RemoveAll(typeof(IDbContextOptionsConfiguration<AppDbContext>));
            services.RemoveAll(typeof(IYardCapacityForecastService));

            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseInMemoryDatabase(_databaseName);
            });

            services.AddSingleton<IYardCapacityForecastService, NoopYardCapacityForecastService>();

            using var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.UseContentRoot(Directory.GetCurrentDirectory());
        return base.CreateHost(builder);
    }
}

internal sealed class NoopYardCapacityForecastService : IYardCapacityForecastService
{
    public Task<YardCapacityForecastResult> ForecastAsync(Guid yardId, int horizonHours, int capacity, CancellationToken cancellationToken = default)
    {
        var generatedAt = DateTime.UtcNow;
        var points = Enumerable.Range(1, Math.Max(1, horizonHours))
            .Select(offset => new YardCapacityForecastPoint(generatedAt.AddHours(offset), 0, 0f))
            .ToList();

        var result = new YardCapacityForecastResult(yardId, generatedAt, capacity, points);
        return Task.FromResult(result);
    }
}
