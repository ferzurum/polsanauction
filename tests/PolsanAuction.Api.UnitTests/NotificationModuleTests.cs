using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using PolsanAuction.Notification.Application;
using PolsanAuction.Notification.Infrastructure;

namespace PolsanAuction.Api.UnitTests;

/// <summary>Console delivery prints confirmation links, so it exists in Development only (spec 0002, R-9/R-10).</summary>
public sealed class NotificationModuleTests
{
    [Fact]
    public void Development_RegistersConsoleDelivery()
    {
        var services = new ServiceCollection().AddNotificationModule(new Environment(Environments.Development));

        using var provider = services.BuildServiceProvider();

        Assert.Equal("ConsoleEmailSender", provider.GetRequiredService<IEmailSender>().GetType().Name);
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    public void OtherEnvironments_RefuseToStart_WithoutConfiguredDelivery(string environment)
    {
        var error = Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddNotificationModule(new Environment(environment)));

        Assert.Contains(environment, error.Message, StringComparison.Ordinal);
    }

    private sealed class Environment(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;

        public string ApplicationName { get; set; } = "PolsanAuction.Api";

        public string ContentRootPath { get; set; } = string.Empty;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
