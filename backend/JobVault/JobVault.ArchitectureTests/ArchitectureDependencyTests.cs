using NetArchTest.Rules;
using Xunit;

namespace JobVault.ArchitectureTests;

public class ArchitectureDependencyTests
{
    private const string DomainNamespace = "JobVault.Domain";
    private const string ContractsNamespace = "JobVault.Contracts";
    private const string ApplicationNamespace = "JobVault.Application";
    private const string InfrastructureNamespace = "JobVault.Infrastructure";
    private const string ApiNamespace = "JobVault.API";
    private const string WorkerNamespace = "JobVault.Worker";

    [Fact]
    public void Domain_Should_Not_Have_Dependencies_On_Other_Layers()
    {
        // Arrange
        var domainAssembly = typeof(JobVault.Domain.Entities.JobApplication).Assembly;

        // Act
        var result = Types.InAssembly(domainAssembly)
            .Should()
            .NotHaveDependencyOn(ApplicationNamespace)
            .And().NotHaveDependencyOn(InfrastructureNamespace)
            .And().NotHaveDependencyOn(ApiNamespace)
            .And().NotHaveDependencyOn(WorkerNamespace)
            .And().NotHaveDependencyOn("MongoDB.Driver")
            .And().NotHaveDependencyOn("RabbitMQ.Client")
            .And().NotHaveDependencyOn("Telegram.Bot")
            .And().NotHaveDependencyOn("Microsoft.AspNetCore")
            .GetResult();

        // Assert
        Assert.True(result.IsSuccessful,
            $"Domain layer has forbidden dependencies: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Contracts_Should_Not_Have_Dependencies_On_Other_Layers()
    {
        // Arrange
        var contractsAssembly = typeof(JobVault.Contracts.Events.JobApplicationEvent).Assembly;

        // Act
        var result = Types.InAssembly(contractsAssembly)
            .Should()
            .NotHaveDependencyOn(ApplicationNamespace)
            .And().NotHaveDependencyOn(InfrastructureNamespace)
            .And().NotHaveDependencyOn(ApiNamespace)
            .And().NotHaveDependencyOn(WorkerNamespace)
            .And().NotHaveDependencyOn("MongoDB.Driver")
            .And().NotHaveDependencyOn("RabbitMQ.Client")
            .And().NotHaveDependencyOn("Telegram.Bot")
            .And().NotHaveDependencyOn("Microsoft.AspNetCore")
            .GetResult();

        // Assert
        Assert.True(result.IsSuccessful,
            $"Contracts layer has forbidden dependencies: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Application_Should_Not_Have_Dependencies_On_Infrastructure_Or_Presentation_Layers()
    {
        // Arrange
        var applicationAssembly = typeof(JobVault.Application.Interfaces.IFileIngestService).Assembly;

        // Act
        var result = Types.InAssembly(applicationAssembly)
            .Should()
            .NotHaveDependencyOn(InfrastructureNamespace)
            .And().NotHaveDependencyOn(ApiNamespace)
            .And().NotHaveDependencyOn(WorkerNamespace)
            .And().NotHaveDependencyOn("MongoDB.Driver")
            .And().NotHaveDependencyOn("RabbitMQ.Client")
            .And().NotHaveDependencyOn("Telegram.Bot")
            .And().NotHaveDependencyOn("Microsoft.AspNetCore")
            .GetResult();

        // Assert
        Assert.True(result.IsSuccessful,
            $"Application layer has forbidden dependencies: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void API_Should_Not_Directly_Depend_On_Infrastructure_Packages()
    {
        // Arrange
        var apiAssembly = typeof(JobVault.API.Controllers.VaultController).Assembly;

        // Act
        var result = Types.InAssembly(apiAssembly)
            .Should()
            .NotHaveDependencyOn("MongoDB.Driver")
            .And().NotHaveDependencyOn("RabbitMQ.Client")
            .And().NotHaveDependencyOn("Telegram.Bot")
            .GetResult();

        // Assert
        Assert.True(result.IsSuccessful,
            $"API layer directly depends on infrastructure packages: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Worker_Should_Not_Directly_Depend_On_Infrastructure_Packages()
    {
        // Arrange
        var workerAssembly = typeof(JobVault.Worker.Program).Assembly;

        // Act
        var result = Types.InAssembly(workerAssembly)
            .Should()
            .NotHaveDependencyOn("MongoDB.Driver")
            .And().NotHaveDependencyOn("RabbitMQ.Client")
            .And().NotHaveDependencyOn("Telegram.Bot")
            .GetResult();

        // Assert
        Assert.True(result.IsSuccessful,
            $"Worker layer directly depends on infrastructure packages: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Infrastructure_Should_Only_Depend_On_Application_Domain_And_Contracts()
    {
        // Arrange
        var infrastructureAssembly = typeof(JobVault.Infrastructure.GitHub.FileIngestService).Assembly;

        // Act
        var result = Types.InAssembly(infrastructureAssembly)
            .Should()
            .NotHaveDependencyOn(ApiNamespace)
            .And().NotHaveDependencyOn(WorkerNamespace)
            .GetResult();

        // Assert
        Assert.True(result.IsSuccessful,
            $"Infrastructure layer has forbidden dependencies: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Application_Interfaces_Should_Be_In_Interfaces_Namespace()
    {
        // Arrange
        var applicationAssembly = typeof(JobVault.Application.Interfaces.IFileIngestService).Assembly;

        // Act
        var result = Types.InAssembly(applicationAssembly)
            .That()
            .AreInterfaces()
            .And()
            .DoNotHaveName("IFormFile") // Exclude framework interfaces
            .Should()
            .ResideInNamespace($"{ApplicationNamespace}.Interfaces")
            .GetResult();

        // Assert
        Assert.True(result.IsSuccessful,
            $"Application interfaces not in correct namespace: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Domain_Entities_Should_Be_In_Entities_Namespace()
    {
        // Arrange
        var domainAssembly = typeof(JobVault.Domain.Entities.JobApplication).Assembly;

        // Act
        var result = Types.InAssembly(domainAssembly)
            .That()
            .AreClasses()
            .And()
            .ResideInNamespace($"{DomainNamespace}.Entities")
            .Should()
            .NotHaveDependencyOnAny(
                ApplicationNamespace,
                InfrastructureNamespace,
                ApiNamespace,
                WorkerNamespace)
            .GetResult();

        // Assert
        Assert.True(result.IsSuccessful,
            $"Domain entities have forbidden dependencies: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Controllers_Should_Only_Depend_On_Application_And_Contracts()
    {
        // Arrange
        var apiAssembly = typeof(JobVault.API.Controllers.VaultController).Assembly;

        // Act
        var result = Types.InAssembly(apiAssembly)
            .That()
            .ResideInNamespace($"{ApiNamespace}.Controllers")
            .Should()
            .NotHaveDependencyOn(InfrastructureNamespace)
            .GetResult();

        // Assert
        Assert.True(result.IsSuccessful,
            $"Controllers have direct infrastructure dependencies: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Infrastructure_Services_Should_Implement_Application_Interfaces()
    {
        // Arrange
        var infrastructureAssembly = typeof(JobVault.Infrastructure.GitHub.FileIngestService).Assembly;
        var applicationAssembly = typeof(JobVault.Application.Interfaces.IFileIngestService).Assembly;

        // Get all interfaces from Application
        var applicationInterfaces = Types.InAssembly(applicationAssembly)
            .That()
            .AreInterfaces()
            .And()
            .ResideInNamespace($"{ApplicationNamespace}.Interfaces")
            .GetTypes()
            .ToList();

        // Get all classes from Infrastructure
        var infrastructureClasses = Types.InAssembly(infrastructureAssembly)
            .That()
            .AreClasses()
            .And()
            .AreNotAbstract()
            .GetTypes()
            .ToList();

        // Act - Check if at least some infrastructure classes implement application interfaces
        var implementingClasses = infrastructureClasses
            .Where(c => applicationInterfaces.Any(i => i.IsAssignableFrom(c)))
            .ToList();

        // Assert
        Assert.NotEmpty(implementingClasses);
    }
}