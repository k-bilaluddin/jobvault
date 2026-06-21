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

    private static readonly string[] InfrastructurePackages =
    [
        "MongoDB.Driver",
        "RabbitMQ.Client",
        "Telegram.Bot",
    ];

    // ─── Layer Isolation ────────────────────────────────────────────

    [Fact]
    public void Domain_Should_Not_Have_Dependencies_On_Other_Layers()
    {
        var domainAssembly = typeof(JobVault.Domain.Entities.JobApplication).Assembly;

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

        Assert.True(result.IsSuccessful,
            $"Domain layer has forbidden dependencies: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Contracts_Should_Not_Have_Dependencies_On_Other_Layers()
    {
        var contractsAssembly = typeof(JobVault.Contracts.Events.JobApplicationEvent).Assembly;

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

        Assert.True(result.IsSuccessful,
            $"Contracts layer has forbidden dependencies: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Application_Should_Not_Have_Dependencies_On_Infrastructure_Or_Presentation_Layers()
    {
        var applicationAssembly = typeof(JobVault.Application.Interfaces.IFileIngestService).Assembly;

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

        Assert.True(result.IsSuccessful,
            $"Application layer has forbidden dependencies: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Infrastructure_Should_Only_Depend_On_Application_Domain_And_Contracts()
    {
        var infrastructureAssembly = typeof(JobVault.Infrastructure.GitHub.FileIngestService).Assembly;

        var result = Types.InAssembly(infrastructureAssembly)
            .Should()
            .NotHaveDependencyOn(ApiNamespace)
            .And().NotHaveDependencyOn(WorkerNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Infrastructure layer has forbidden dependencies: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    // ─── Presentation Layer Isolation ───────────────────────────────

    [Fact]
    public void API_Should_Not_Directly_Depend_On_Infrastructure_Packages()
    {
        var apiAssembly = typeof(JobVault.API.Controllers.VaultController).Assembly;

        var result = Types.InAssembly(apiAssembly)
            .Should()
            .NotHaveDependencyOn("MongoDB.Driver")
            .And().NotHaveDependencyOn("RabbitMQ.Client")
            .And().NotHaveDependencyOn("Telegram.Bot")
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"API layer directly depends on infrastructure packages: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Worker_Should_Not_Directly_Depend_On_Infrastructure_Packages()
    {
        var workerAssembly = typeof(JobVault.Worker.Program).Assembly;

        var result = Types.InAssembly(workerAssembly)
            .Should()
            .NotHaveDependencyOn("MongoDB.Driver")
            .And().NotHaveDependencyOn("RabbitMQ.Client")
            .And().NotHaveDependencyOn("Telegram.Bot")
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Worker layer directly depends on infrastructure packages: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Controllers_Should_Not_Depend_On_Infrastructure()
    {
        var apiAssembly = typeof(JobVault.API.Controllers.VaultController).Assembly;

        var result = Types.InAssembly(apiAssembly)
            .That()
            .ResideInNamespace($"{ApiNamespace}.Controllers")
            .Should()
            .NotHaveDependencyOn(InfrastructureNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Controllers have direct infrastructure dependencies: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    // ─── Namespace Conventions ──────────────────────────────────────

    [Fact]
    public void Application_Interfaces_Should_Be_In_Interfaces_Namespace()
    {
        var applicationAssembly = typeof(JobVault.Application.Interfaces.IFileIngestService).Assembly;

        var result = Types.InAssembly(applicationAssembly)
            .That()
            .AreInterfaces()
            .And()
            .DoNotHaveName("IFormFile")
            .Should()
            .ResideInNamespace($"{ApplicationNamespace}.Interfaces")
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Application interfaces not in correct namespace: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Application_Services_Should_Be_In_Services_Namespace()
    {
        var applicationAssembly = typeof(JobVault.Application.Interfaces.IFileIngestService).Assembly;

        var result = Types.InAssembly(applicationAssembly)
            .That()
            .AreClasses()
            .And()
            .AreNotAbstract()
            .And()
            .DoNotResideInNamespace($"{ApplicationNamespace}.Common")
            .And()
            .DoNotResideInNamespace($"{ApplicationNamespace}.Models")
            .And()
            .DoNotResideInNamespace($"{ApplicationNamespace}.Interfaces")
            .Should()
            .ResideInNamespace($"{ApplicationNamespace}.Services")
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Application classes outside Services namespace: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Domain_Entities_Should_Be_In_Entities_Namespace()
    {
        var domainAssembly = typeof(JobVault.Domain.Entities.JobApplication).Assembly;

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

        Assert.True(result.IsSuccessful,
            $"Domain entities have forbidden dependencies: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    // ─── Controller Conventions ─────────────────────────────────────

    [Fact]
    public void Controllers_Should_Inherit_From_ControllerBase()
    {
        var apiAssembly = typeof(JobVault.API.Controllers.VaultController).Assembly;

        var result = Types.InAssembly(apiAssembly)
            .That()
            .ResideInNamespace($"{ApiNamespace}.Controllers")
            .And()
            .AreClasses()
            .Should()
            .Inherit(typeof(Microsoft.AspNetCore.Mvc.ControllerBase))
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Controllers not inheriting ControllerBase: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Controllers_Should_Have_Controller_Suffix()
    {
        var apiAssembly = typeof(JobVault.API.Controllers.VaultController).Assembly;

        var result = Types.InAssembly(apiAssembly)
            .That()
            .ResideInNamespace($"{ApiNamespace}.Controllers")
            .And()
            .AreClasses()
            .Should()
            .HaveNameEndingWith("Controller")
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Controllers missing 'Controller' suffix: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    // ─── Controller Clean Architecture Enforcement ───────────────────

    [Fact]
    public void Controllers_Should_Not_Depend_On_Repositories()
    {
        var apiAssembly = typeof(JobVault.API.Controllers.VaultController).Assembly;

        var result = Types.InAssembly(apiAssembly)
            .That()
            .ResideInNamespace($"{ApiNamespace}.Controllers")
            .And()
            .AreClasses()
            .Should()
            .NotHaveDependencyOn("JobVault.Application.Interfaces.IJobApplicationRepository")
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Controllers depend on repositories directly: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Controllers_Should_Not_Use_FileSystem()
    {
        var apiAssembly = typeof(JobVault.API.Controllers.VaultController).Assembly;

        var result = Types.InAssembly(apiAssembly)
            .That()
            .ResideInNamespace($"{ApiNamespace}.Controllers")
            .And()
            .AreClasses()
            .Should()
            .NotHaveDependencyOn("System.IO.Directory")
            .And().NotHaveDependencyOn("System.IO.File")
            .And().NotHaveDependencyOn("System.IO.Path")
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Controllers use file system APIs directly: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Controllers_Should_Not_Use_ProcessApis()
    {
        var apiAssembly = typeof(JobVault.API.Controllers.VaultController).Assembly;

        var result = Types.InAssembly(apiAssembly)
            .That()
            .ResideInNamespace($"{ApiNamespace}.Controllers")
            .And()
            .AreClasses()
            .Should()
            .NotHaveDependencyOn("System.Diagnostics.Process")
            .And().NotHaveDependencyOn("System.Diagnostics.ProcessStartInfo")
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Controllers use Process APIs directly: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Controllers_Should_Not_Depend_On_Markdig()
    {
        var apiAssembly = typeof(JobVault.API.Controllers.VaultController).Assembly;

        var result = Types.InAssembly(apiAssembly)
            .That()
            .ResideInNamespace($"{ApiNamespace}.Controllers")
            .And()
            .AreClasses()
            .Should()
            .NotHaveDependencyOn("Markdig")
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Controllers depend on Markdig directly: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Controllers_Should_Not_Depend_On_JwtHandlers()
    {
        var apiAssembly = typeof(JobVault.API.Controllers.VaultController).Assembly;

        var result = Types.InAssembly(apiAssembly)
            .That()
            .ResideInNamespace($"{ApiNamespace}.Controllers")
            .And()
            .AreClasses()
            .Should()
            .NotHaveDependencyOn("System.IdentityModel.Tokens.Jwt")
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Controllers depend on JWT handlers directly: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    // ─── Interface Implementation Coverage ──────────────────────────

    [Fact]
    public void Every_Application_Interface_Should_Have_An_Implementation()
    {
        var applicationAssembly = typeof(JobVault.Application.Interfaces.IFileIngestService).Assembly;
        var infrastructureAssembly = typeof(JobVault.Infrastructure.GitHub.FileIngestService).Assembly;

        var applicationInterfaces = Types.InAssembly(applicationAssembly)
            .That()
            .AreInterfaces()
            .And()
            .ResideInNamespace($"{ApplicationNamespace}.Interfaces")
            .GetTypes()
            .ToList();

        var allClasses = Types.InAssembly(applicationAssembly)
            .That().AreClasses().And().AreNotAbstract()
            .GetTypes()
            .Concat(
                Types.InAssembly(infrastructureAssembly)
                    .That().AreClasses().And().AreNotAbstract()
                    .GetTypes())
            .ToList();

        var unimplemented = applicationInterfaces
            .Where(iface => !allClasses.Any(cls => iface.IsAssignableFrom(cls)))
            .Select(i => i.Name)
            .ToList();

        Assert.True(unimplemented.Count == 0,
            $"Application interfaces without implementations: {string.Join(", ", unimplemented)}");
    }

    [Fact]
    public void Infrastructure_Implementations_Should_Only_Implement_Application_Interfaces()
    {
        var infrastructureAssembly = typeof(JobVault.Infrastructure.GitHub.FileIngestService).Assembly;
        var applicationAssembly = typeof(JobVault.Application.Interfaces.IFileIngestService).Assembly;

        var applicationInterfaces = Types.InAssembly(applicationAssembly)
            .That()
            .AreInterfaces()
            .And()
            .ResideInNamespace($"{ApplicationNamespace}.Interfaces")
            .GetTypes()
            .ToHashSet();

        var implementingClasses = Types.InAssembly(infrastructureAssembly)
            .That()
            .AreClasses()
            .And()
            .AreNotAbstract()
            .GetTypes()
            .Where(c => applicationInterfaces.Any(i => i.IsAssignableFrom(c)))
            .ToList();

        Assert.NotEmpty(implementingClasses);

        foreach (var cls in implementingClasses)
        {
            var customInterfaces = cls.GetInterfaces()
                .Where(i => i.Namespace != null
                    && !i.Namespace.StartsWith("System")
                    && !i.Namespace.StartsWith("Microsoft")
                    && !i.Namespace.StartsWith(ApplicationNamespace));

            Assert.True(!customInterfaces.Any(),
                $"{cls.Name} implements non-Application interface: {string.Join(", ", customInterfaces.Select(i => i.FullName))}");
        }
    }
}
