using System.Runtime.InteropServices;
using Xunit;

namespace MiteAI.Tests;

public class InfrastructureTests
{
    [Fact]
    public void BuildInfrastructure_ResolvesWindowsPlatform()
    {
        Assert.True(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));
    }
}
