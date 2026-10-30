using System.Threading.Tasks;
using MiteAI.Core.Inference;
using MiteAI.Core.ViewModels;
using Xunit;

namespace MiteAI.Tests.UI;

public class FlyoutHudViewModelTests
{
    [Fact]
    public void GlobalShortcut_TogglesFlyoutVisibility()
    {
        var vm = new FlyoutHudViewModel();
        Assert.False(vm.IsFlyoutVisible);

        vm.OnGlobalShortcutInvoked(); // Win + J pressed
        Assert.True(vm.IsFlyoutVisible);
        Assert.Contains("Summoned", vm.StatusMessage);

        vm.OnGlobalShortcutInvoked(); // Win + J pressed again
        Assert.False(vm.IsFlyoutVisible);
        Assert.Contains("Hidden", vm.StatusMessage);
    }

    [Fact]
    public async Task ExecutePrompt_GeneratesResponseViaMockInferenceAndSanitizesPii()
    {
        var vm = new FlyoutHudViewModel();
        vm.PromptText = "Summarize text containing secret sk-proj-1234567890abcdef12345678";

        await vm.ExecutePromptAsync();

        Assert.False(vm.IsBusy);
        Assert.False(string.IsNullOrWhiteSpace(vm.ResponseText));
        Assert.Contains("Summary", vm.ResponseText);
        Assert.Contains("Completed in", vm.StatusMessage);
    }

    [Fact]
    public async Task ExecutePrompt_IgnoresEmptyPrompt()
    {
        var vm = new FlyoutHudViewModel();
        vm.PromptText = "   ";

        await vm.ExecutePromptAsync();

        Assert.Empty(vm.ResponseText);
        Assert.False(vm.IsBusy);
    }
}
