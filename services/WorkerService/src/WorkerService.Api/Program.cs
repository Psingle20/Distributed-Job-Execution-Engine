using System.Globalization;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using WorkerService.Api.Infrastructure;
using WorkerService.Api.Middleware;
using WorkerService.Application.Observability;
using WorkerService.Application;
using WorkerService.Domain;
using WorkerService.Infrastructure;
using WorkerService.Infrastructure.Persistence;
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
        .ConfigureResource(resource => resource.AddService(WorkerServiceDiagnostics.ServiceName))
        .WithTracing(tracing => tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddSource(WorkerServiceDiagnostics.ServiceName)
            .AddSource("Dapr.EntityFrameworkCore.Outbox")
            .AddOtlpExporter())
        .WithMetrics(metrics => metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddMeter(WorkerServiceDiagnostics.ServiceName)
            .AddOtlpExporter());

    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy => policy
            .WithOrigins("http://localhost:5000")
            .AllowAnyHeader()
            .AllowAnyMethod());
    });

    builder.Services.AddDaprClient();

    new DomainModule().RegisterServices(builder.Services, builder.Configuration);
    new ApplicationModule().RegisterServices(builder.Services, builder.Configuration);
    new InfrastructureModule().RegisterServices(builder.Services, builder.Configuration);

    WebApplication app = builder.Build();

    using (IServiceScope scope = app.Services.CreateScope())
    {
        WorkerDbContext dbContext = scope.ServiceProvider.GetRequiredService<WorkerDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    app.UseMiddleware<RequestContextLoggingMiddleware>();
    app.UseSerilogRequestLogging();
    app.UseExceptionHandler();
    app.UseCors();
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
