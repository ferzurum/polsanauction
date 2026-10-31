using System.Globalization;
using System.Runtime.CompilerServices;

namespace PolsanAuction.Tests;

/// <summary>Fixes the culture for every test assembly (docs/testing.md, Determinism).</summary>
internal static class TestCulture
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        var culture = CultureInfo.GetCultureInfo("tr-TR");
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
    }
}
