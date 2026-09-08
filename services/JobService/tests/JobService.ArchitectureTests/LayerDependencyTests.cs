using System.Reflection;
using NetArchTest.Rules;
using Shouldly;

namespace JobService.ArchitectureTests;

public sealed class LayerDependencyTests
{
    private static readonly Assembly DomainAssembly = typeof(Domain.Jobs.Job).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(Application.ApplicationModule).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(Infrastructure.InfrastructureModule).Assembly;
    [Fact]
    public void Domain_ShouldNotReference_Application()
    {
        Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn("JobService.Application")
            .GetResult()
            .IsSuccessful.ShouldBeTrue();
    }

    [Fact]
    public void Domain_ShouldNotReference_Infrastructure()
    {
        Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn("JobService.Infrastructure")
            .GetResult()
            .IsSuccessful.ShouldBeTrue();
    }

    [Fact]
    public void Domain_ShouldNotReference_Api()
    {
        Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn("JobService.Api")
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
    public void Application_ShouldNotReference_Infrastructure()
    {
        Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOn("JobService.Infrastructure")
            .GetResult()
            .IsSuccessful.ShouldBeTrue();
    }

    [Fact]
    public void Application_ShouldNotReference_Api()
    {
        Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOn("JobService.Api")
            .GetResult()
            .IsSuccessful.ShouldBeTrue();
    }

    [Fact]
    public void Infrastructure_ShouldNotReference_Api()
    {
        Types.InAssembly(InfrastructureAssembly)
            .ShouldNot()
            .HaveDependencyOn("JobService.Api")
            .GetResult()
            .IsSuccessful.ShouldBeTrue();
    }
}
