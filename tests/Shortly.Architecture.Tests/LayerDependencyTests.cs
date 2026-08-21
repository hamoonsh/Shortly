using System.Reflection;
using NetArchTest.Rules;

namespace Shortly.Architecture.Tests;

/// <summary>
/// Enforces the Clean Architecture dependency rule:
/// Domain ← Application ← Infrastructure ← Api/Functions.
/// These tests gate every later phase — they must stay green from Phase 0 onward.
/// </summary>
public class LayerDependencyTests
{
    private static readonly Assembly DomainAssembly = typeof(Shortly.Domain.AssemblyMarker).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(Shortly.Application.AssemblyMarker).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(Shortly.Infrastructure.AssemblyMarker).Assembly;

    private const string DomainNamespace = "Shortly.Domain";
    private const string ApplicationNamespace = "Shortly.Application";
    private const string InfrastructureNamespace = "Shortly.Infrastructure";
    private const string ApiNamespace = "Shortly.Api";
    private const string FunctionsNamespace = "Shortly.Functions";

    [Fact]
    public void Domain_Should_Not_Depend_On_Any_Other_Layer()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(ApplicationNamespace, InfrastructureNamespace, ApiNamespace, FunctionsNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful, FailureMessage(result));
    }

    [Fact]
    public void Domain_Should_Not_Depend_On_External_Frameworks()
    {
        // Domain references nothing: no EF Core, no ASP.NET Core, no Azure SDKs, no MediatR.
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Microsoft.EntityFrameworkCore",
                "Microsoft.AspNetCore",
                "Azure",
                "MediatR")
            .GetResult();

        Assert.True(result.IsSuccessful, FailureMessage(result));
    }

    [Fact]
    public void Application_Should_Only_Depend_On_Domain()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(InfrastructureNamespace, ApiNamespace, FunctionsNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful, FailureMessage(result));
    }

    [Fact]
    public void Application_Should_Not_Depend_On_External_Frameworks()
    {
        // Persistence and transport concerns belong to Infrastructure, not Application.
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Microsoft.EntityFrameworkCore",
                "Microsoft.AspNetCore",
                "Azure.Messaging")
            .GetResult();

        Assert.True(result.IsSuccessful, FailureMessage(result));
    }

    [Fact]
    public void Infrastructure_Should_Not_Depend_On_Api_Or_Functions()
    {
        var result = Types.InAssembly(InfrastructureAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(ApiNamespace, FunctionsNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful, FailureMessage(result));
    }

    [Fact]
    public void Application_Handlers_Should_Not_Depend_On_Concrete_Infrastructure()
    {
        // Handlers must depend on abstractions (ILinkRepository, IEventPublisher) only.
        // Vacuously green until handlers exist in Phase 1; it bites from then on.
        var result = Types.InAssembly(ApplicationAssembly)
            .That().HaveNameEndingWith("Handler")
            .ShouldNot()
            .HaveDependencyOn(InfrastructureNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful, FailureMessage(result));
    }

    private static string FailureMessage(TestResult result) =>
        "Offending types: " + string.Join(", ", result.FailingTypeNames ?? Enumerable.Empty<string>());
}
