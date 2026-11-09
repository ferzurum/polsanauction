using System.Text.RegularExpressions;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace PolsanAuction.ArchitectureTests;

/// <summary>docs/architecture.md, forbidden dependency 1: Domain never depends on infrastructure.</summary>
public sealed class LayerDependencyTests
{
    private static readonly string[] InfrastructureNamespaces =
    [
        "Microsoft.EntityFrameworkCore",
        "Microsoft.AspNetCore", // includes Microsoft.AspNetCore.Identity (UserManager, IdentityUser)
        "Microsoft.Extensions.Identity",
        "Npgsql",
        "System.Net.Mail",
        "MailKit",
    ];

    [Theory]
    [MemberData(nameof(Modules.ModuleNames), MemberType = typeof(Modules))]
    public void Domain_DoesNotDependOnInfrastructurePackages(string module)
    {
        foreach (var forbidden in InfrastructureNamespaces)
        {
            var rule = Types().That().ResideInAssembly(Modules.Get(module, "Domain"))
                .Should().NotDependOnAnyTypesThat().ResideInNamespaceMatching($@"^{Regex.Escape(forbidden)}(\..+)?$");

            Assert.True(rule.HasNoViolations(Modules.Architecture), $"{module}.Domain depends on {forbidden}.");
        }
    }

    [Theory]
    [MemberData(nameof(Modules.ModuleNames), MemberType = typeof(Modules))]
    public void InnerLayers_DoNotDependOnAnyInfrastructureLayer(string module)
    {
        foreach (var layer in new[] { "Domain", "Application" })
        {
            foreach (var other in Modules.Names)
            {
                var rule = Types().That().ResideInAssembly(Modules.Get(module, layer))
                    .Should().NotDependOnAnyTypesThat().ResideInAssembly(Modules.Get(other, "Infrastructure"));

                Assert.True(rule.HasNoViolations(Modules.Architecture), $"{module}.{layer} depends on {other}.Infrastructure.");
            }
        }
    }
}
