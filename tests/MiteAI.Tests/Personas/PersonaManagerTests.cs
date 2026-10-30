using MiteAI.Core.Personas;
using Xunit;

namespace MiteAI.Tests.Personas;

public class PersonaManagerTests
{
    [Fact]
    public void ResolvePersona_ReturnsMappedPersonaForKnownApplication()
    {
        var manager = new PersonaManager();

        var vsPersona = manager.ResolvePersonaForProcess("devenv.exe");
        Assert.Equal("Strict C++ Code Reviewer", vsPersona.Name);
        Assert.Contains("C++", vsPersona.SystemPrompt);

        var wordPersona = manager.ResolvePersonaForProcess("WINWORD.EXE");
        Assert.Equal("Creative Copywriter", wordPersona.Name);
        Assert.Contains("copywriter", wordPersona.SystemPrompt);
    }

    [Fact]
    public void ResolvePersona_FallsBackToDefaultWhenUnmapped()
    {
        var manager = new PersonaManager();

        var defaultPersona = manager.ResolvePersonaForProcess("unknown_app.exe");
        Assert.Equal("General Assistant", defaultPersona.Name);
    }

    [Fact]
    public void SetExplicitOverride_OverridesProcessMapping()
    {
        var manager = new PersonaManager();
        manager.SetActiveOverride("Creative Copywriter");

        var persona = manager.ResolvePersonaForProcess("devenv.exe");
        Assert.Equal("Creative Copywriter", persona.Name);

        manager.ClearActiveOverride();
        var restoredPersona = manager.ResolvePersonaForProcess("devenv.exe");
        Assert.Equal("Strict C++ Code Reviewer", restoredPersona.Name);
    }
}
