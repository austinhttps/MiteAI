using MiteAI.Core.Context;
using MiteAI.Core.Privacy;
using Xunit;

namespace MiteAI.Tests.Context;

public class ActiveWindowContextServiceTests
{
    [Fact]
    public void GetCurrentContext_ReturnsContextForAllowedApplication()
    {
        var blindspots = new ContextBlindspotService();
        var contextService = new ActiveWindowContextService(blindspots);

        var context = contextService.CreateContext("Program.cs - Visual Studio Code", "code.exe", "int x = 42;");

        Assert.False(context.IsSuppressed);
        Assert.Equal("code.exe", context.ProcessName);
        Assert.Equal("int x = 42;", context.SelectedText);
    }

    [Fact]
    public void GetCurrentContext_SuppressesDataWhenSensitiveAppFocused()
    {
        var blindspots = new ContextBlindspotService();
        var contextService = new ActiveWindowContextService(blindspots);

        var context = contextService.CreateContext("Personal Vault - 1Password", "1Password.exe", "my_master_password");

        Assert.True(context.IsSuppressed);
        Assert.Null(context.SelectedText);
        Assert.Equal("[Context Suppressed - Sensitive Application Active]", context.StatusSummary);
    }
}
