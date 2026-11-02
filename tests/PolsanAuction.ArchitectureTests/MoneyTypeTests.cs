using System.Reflection;

namespace PolsanAuction.ArchitectureTests;

/// <summary>docs/architecture.md, forbidden dependency 6: money is whole kuruş in a <c>long</c>, never floating point.</summary>
public sealed class MoneyTypeTests
{
    // Matched case-insensitively, so private _camelCase fields (_price, _amountKurus) are covered too.
    private static readonly string[] MoneyNameParts = ["Amount", "Price", "Kurus", "Kuruş"];

    private const BindingFlags AllMembers =
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    [Fact]
    public void MoneyMembers_AreWholeKurusLongs()
    {
        var offenders = Modules.All
            .Append(typeof(Program).Assembly)
            .SelectMany(assembly => assembly.GetTypes())
            .SelectMany(type => MoneyMembers(type).Select(member => (type, member.Name, member.Type)))
            .Where(found => Nullable.GetUnderlyingType(found.Type) is not { } inner ? found.Type != typeof(long) : inner != typeof(long))
            .Select(found => $"{found.type.FullName}.{found.Name} : {found.Type.Name}")
            .ToList();

        Assert.Empty(offenders);
    }

    private static IEnumerable<(string Name, Type Type)> MoneyMembers(Type type) =>
        type.GetProperties(AllMembers).Select(property => (property.Name, property.PropertyType))
            .Concat(type.GetFields(AllMembers)
                .Where(field => !field.IsDefined(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute)))
                .Select(field => (field.Name, field.FieldType)))
            .Where(member => MoneyNameParts.Any(part => member.Name.Contains(part, StringComparison.OrdinalIgnoreCase)));
}
