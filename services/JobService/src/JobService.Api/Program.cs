using System.Globalization;
using JobService.Api.Infrastructure;
using JobService.Api.Middleware;
using JobService.Application.Observability;
using JobService.Application;
using JobService.Domain;
using JobService.Infrastructure;
using JobService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
    .CreateBootstrapLogger();

try
{
    WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, configuration) =>
        configuration.ReadFrom.Configuration(context.Configuration));

    builder.Services.AddControllers()
        .AddJsonOptions(options =>
            options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();

    builder.Services.AddOpenTelemetry()
        .ConfigureResource(resource => resource.AddService(JobServiceDiagnostics.ServiceName))
        .WithTracing(tracing => tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddSource(JobServiceDiagnostics.ServiceName)
            .AddSource("Dapr.EntityFrameworkCore.Outbox")
            .AddOtlpExporter())
        .WithMetrics(metrics => metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddMeter(JobServiceDiagnostics.ServiceName)
            .AddOtlpExporter());

    builder.Services.AddDaprClient();

    new DomainModule().RegisterServices(builder.Services, builder.Configuration);
    new ApplicationModule().RegisterServices(builder.Services, builder.Configuration);
    new InfrastructureModule().RegisterServices(builder.Services, builder.Configuration);

    WebApplication app = builder.Build();

    using (IServiceScope scope = app.Services.CreateScope())
    {
        JobDbContext dbContext = scope.ServiceProvider.GetRequiredService<JobDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    app.UseMiddleware<RequestContextLoggingMiddleware>();
    app.UseSerilogRequestLogging();
    app.UseExceptionHandler();
    app.UseDefaultFiles();
    app.UseStaticFiles();
    app.UseCloudEvents();

    app.MapSubscribeHandler();
    app.MapGet("/health", () => Results.Ok(new { Status = "Healthy" }));
    app.MapControllers();

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}
