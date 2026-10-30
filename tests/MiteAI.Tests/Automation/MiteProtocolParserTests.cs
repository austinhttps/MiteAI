using MiteAI.Core.Automation;
using Xunit;

namespace MiteAI.Tests.Automation;

public class MiteProtocolParserTests
{
    [Fact]
    public void ParseUri_ExtractsPromptParametersAccurately()
    {
        var parser = new MiteProtocolParser();
        var uri = "mite://prompt?text=Summarize%20this%20document&system=concise";

        var command = parser.Parse(uri);

        Assert.NotNull(command);
        Assert.Equal(ProtocolActionType.Prompt, command.Action);
        Assert.Equal("Summarize this document", command.Text);
        Assert.Equal("concise", command.Parameters["system"]);
    }

    [Fact]
    public void ParseUri_HandlesInvalidSchemeSafely()
    {
        var parser = new MiteProtocolParser();
        var command = parser.Parse("https://example.com");

        Assert.Null(command);
    }

    [Fact]
    public void ParseUri_HandlesMalformedUriSafely()
    {
        var parser = new MiteProtocolParser();
        var command = parser.Parse("not-a-valid-uri");

        Assert.Null(command);
    }
}
