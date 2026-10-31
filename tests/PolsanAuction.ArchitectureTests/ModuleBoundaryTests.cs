using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace PolsanAuction.ArchitectureTests;

/// <summary>docs/architecture.md, forbidden dependencies 2 and 3.</summary>
public sealed class ModuleBoundaryTests
{
    [Theory]
    [MemberData(nameof(Modules.ModuleNames), MemberType = typeof(Modules))]
    public void Module_ReachesOtherModulesOnlyThroughTheirApplicationLayer(string module)
    {
        foreach (var layer in Modules.Layers)
        {
            foreach (var other in Modules.Names.Where(name => name != module))
            {
                foreach (var hiddenLayer in new[] { "Domain", "Infrastructure" })
                {
                    var rule = Types().That().ResideInAssembly(Modules.Get(module, layer))
                        .Should().NotDependOnAnyTypesThat().ResideInAssembly(Modules.Get(other, hiddenLayer));

                    Assert.True(
                        rule.HasNoViolations(Modules.Architecture),
                        $"{module}.{layer} depends on {other}.{hiddenLayer}.");
                }
            }
        }
    }

    [Fact]
    public void Catalog_DoesNotDependOnAuction()
    {
        foreach (var layer in Modules.Layers)
        {
            foreach (var auctionLayer in Modules.Layers)
            {
                var rule = Types().That().ResideInAssembly(Modules.Get("Catalog", layer))
                    .Should().NotDependOnAnyTypesThat().ResideInAssembly(Modules.Get("Auction", auctionLayer));

                Assert.True(rule.HasNoViolations(Modules.Architecture), $"Catalog.{layer} depends on Auction.{auctionLayer}.");
            }
        }
    }
}
