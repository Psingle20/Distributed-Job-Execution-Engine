using System.Reflection;
using NetArchTest.Rules;
using Shouldly;

namespace WorkerService.ArchitectureTests;

public sealed class LayerDependencyTests
{
    private static readonly Assembly DomainAssembly = typeof(Domain.Workers.WorkerExecution).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(Application.ApplicationModule).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(Infrastructure.InfrastructureModule).Assembly;
    [Fact]
    public void Domain_ShouldNotReference_Application()
    {
        Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn("WorkerService.Application")
            .GetResult()
            .IsSuccessful.ShouldBeTrue();
    }

    [Fact]
    public void Domain_ShouldNotReference_Infrastructure()
    {
        Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn("WorkerService.Infrastructure")
            .GetResult()
            .IsSuccessful.ShouldBeTrue();
    }

    [Fact]
    public void Domain_ShouldNotReference_Api()
    {
        Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn("WorkerService.Api")
            .GetResult()
            .IsSuccessful.ShouldBeTrue();
    }

    [Fact]
    public void Domain_ShouldNotReference_EntityFrameworkCore()
    {
        Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn("Microsoft.EntityFrameworkCore")
            .GetResult()
            .IsSuccessful.ShouldBeTrue();
    }

    [Fact]
    public void Domain_ShouldNotReference_Dapr()
    {
        Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn("Dapr.Client")
            .GetResult()
            .IsSuccessful.ShouldBeTrue();
    }

    [Fact]
    public void Domain_ShouldNotReference_JobServiceDomain()
    {
        Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn("JobService.Domain")
            .GetResult()
            .IsSuccessful.ShouldBeTrue();
    }

    [Fact]
    public void Application_ShouldNotReference_Infrastructure()
    {
        Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOn("WorkerService.Infrastructure")
            .GetResult()
            .IsSuccessful.ShouldBeTrue();
    }

    [Fact]
    public void Application_ShouldNotReference_Api()
    {
        Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOn("WorkerService.Api")
            .GetResult()
            .IsSuccessful.ShouldBeTrue();
    }

    [Fact]
    public void Infrastructure_ShouldNotReference_Api()
    {
        Types.InAssembly(InfrastructureAssembly)
            .ShouldNot()
            .HaveDependencyOn("WorkerService.Api")
            .GetResult()
            .IsSuccessful.ShouldBeTrue();
    }
}
