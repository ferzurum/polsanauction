using ArchUnitNET.Loader;
using Architecture = ArchUnitNET.Domain.Architecture;
using Assembly = System.Reflection.Assembly;

namespace PolsanAuction.ArchitectureTests;

/// <summary>The module × layer assemblies of docs/architecture.md, loaded once for every rule.</summary>
internal static class Modules
{
    public static readonly string[] Names = ["Catalog", "Auction", "Identity", "Notification", "Audit"];

    public static readonly string[] Layers = ["Domain", "Application", "Infrastructure"];

    public static readonly Assembly[] All =
    [
        typeof(Catalog.Domain.DomainAssemblyMarker).Assembly,
        typeof(Catalog.Application.ApplicationAssemblyMarker).Assembly,
        typeof(Catalog.Infrastructure.InfrastructureAssemblyMarker).Assembly,
        typeof(Auction.Domain.DomainAssemblyMarker).Assembly,
        typeof(Auction.Application.ApplicationAssemblyMarker).Assembly,
        typeof(Auction.Infrastructure.InfrastructureAssemblyMarker).Assembly,
        typeof(Identity.Domain.DomainAssemblyMarker).Assembly,
        typeof(Identity.Application.ApplicationAssemblyMarker).Assembly,
        typeof(Identity.Infrastructure.InfrastructureAssemblyMarker).Assembly,
        typeof(Notification.Domain.DomainAssemblyMarker).Assembly,
        typeof(Notification.Application.ApplicationAssemblyMarker).Assembly,
        typeof(Notification.Infrastructure.InfrastructureAssemblyMarker).Assembly,
        typeof(Audit.Domain.DomainAssemblyMarker).Assembly,
        typeof(Audit.Application.ApplicationAssemblyMarker).Assembly,
        typeof(Audit.Infrastructure.InfrastructureAssemblyMarker).Assembly,
    ];

    public static readonly Architecture Architecture = new ArchLoader().LoadAssemblies(All).Build();

    public static Assembly Get(string module, string layer) =>
        All.Single(assembly => assembly.GetName().Name == $"PolsanAuction.{module}.{layer}");

    public static TheoryData<string> ModuleNames() => [.. Names];
}
