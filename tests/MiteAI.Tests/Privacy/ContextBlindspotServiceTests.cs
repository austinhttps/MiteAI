using MiteAI.Core.Privacy;
using Xunit;

namespace MiteAI.Tests.Privacy;

public class ContextBlindspotServiceTests
{
    [Theory]
    [InlineData("1Password.exe", "Vault - Personal", true)]
    [InlineData("bitwarden.exe", "Bitwarden", true)]
    [InlineData("KEEPASS.EXE", "Passwords Database", true)]
    [InlineData("chrome.exe", "Chase Online Banking - Google Chrome", true)]
    [InlineData("msedge.exe", "Wells Fargo Commercial - Microsoft Edge", true)]
    [InlineData("code.exe", "Program.cs - Visual Studio Code", false)]
    [InlineData("notepad.exe", "notes.txt - Notepad", false)]
    public void IsExcluded_AccuratelyDetectsSensitiveApps(string process, string title, bool expected)
    {
        var service = new ContextBlindspotService();
        var result = service.IsExcluded(title, process);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void AddCustomExclusion_EnforcesNewBlindspotRule()
    {
        var service = new ContextBlindspotService();
        Assert.False(service.IsExcluded("My Secret App", "custom_vault.exe"));

        service.AddProcessExclusion("custom_vault.exe");
        Assert.True(service.IsExcluded("My Secret App", "custom_vault.exe"));
    }

    [Fact]
    public void IsExcluded_HandlesNullAndEmptySafely()
    {
        var service = new ContextBlindspotService();

        Assert.False(service.IsExcluded(null, null));
        Assert.False(service.IsExcluded("", ""));
    }
}
