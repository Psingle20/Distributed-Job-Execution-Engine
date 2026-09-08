using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace JobService.Infrastructure.Persistence;

[SuppressMessage("Sonar", "S2068", Justification = "Design-time factory for local EF migrations only")]
internal sealed class JobDbContextFactory : IDesignTimeDbContextFactory<JobDbContext>
{
    public JobDbContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<JobDbContext> optionsBuilder = new();
        optionsBuilder
            .UseNpgsql("Host=localhost;Port=5432;Database=jobservice_db;Username=postgres;Password=postgres")
            .UseSnakeCaseNamingConvention();

        return new JobDbContext(optionsBuilder.Options);
    }
}
