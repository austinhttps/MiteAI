using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using MiteAI.Core.ViewModels;

namespace MiteAI.App;

public sealed partial class MainWindow : Window
{
    private readonly FlyoutHudViewModel _viewModel;

    public MainWindow()
    {
        this.InitializeComponent();
        _viewModel = new FlyoutHudViewModel();
    }

    private async void ExecuteButton_Click(object sender, RoutedEventArgs e)
    {
        await RunInferenceAsync();
    }

    private async void PromptInput_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            await RunInferenceAsync();
        }
    }

    private async System.Threading.Tasks.Task RunInferenceAsync()
    {
        _viewModel.PromptText = PromptInput.Text;
        await _viewModel.ExecutePromptAsync();
        ResponseOutput.Text = _viewModel.ResponseText;
        StatusFooter.Text = $"Status: {_viewModel.StatusMessage}";
    }
}
