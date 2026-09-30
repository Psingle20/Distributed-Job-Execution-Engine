using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace WorkerService.Infrastructure.Persistence;

[SuppressMessage("Sonar", "S2068", Justification = "Design-time factory for local EF migrations only")]
internal sealed class WorkerDbContextFactory : IDesignTimeDbContextFactory<WorkerDbContext>
{
    public WorkerDbContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<WorkerDbContext> optionsBuilder = new();
        optionsBuilder
            .UseNpgsql("Host=127.0.0.1;Port=5432;Database=workerservice_db;Username=postgres;Password=postgres")
            .UseSnakeCaseNamingConvention();

        return new WorkerDbContext(optionsBuilder.Options);
    }
}
