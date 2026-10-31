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
        _viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(FlyoutHudViewModel.ResponseText))
            {
                ResponseOutput.Text = _viewModel.ResponseText;
            }
            else if (e.PropertyName == nameof(FlyoutHudViewModel.StatusMessage))
            {
                StatusFooter.Text = $"Status: {_viewModel.StatusMessage}";
            }
        };
    }

    private async void ExecuteButton_Click(object sender, RoutedEventArgs e)
    {
        await RunInferenceStreamingAsync();
    }

    private async void PromptInput_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            await RunInferenceStreamingAsync();
        }
    }

    private async System.Threading.Tasks.Task RunInferenceStreamingAsync()
    {
        _viewModel.PromptText = PromptInput.Text;
        await _viewModel.ExecutePromptStreamingAsync();
    }
}
