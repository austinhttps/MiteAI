using System;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using MiteAI.Core.Inference;
using MiteAI.Core.ViewModels;
using WinRT.Interop;

namespace MiteAI.App;

public sealed partial class MainWindow : Window
{
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TOOLWINDOW = 0x00000080;

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    private readonly FlyoutHudViewModel _viewModel;
    private readonly InferenceEngine _inferenceEngine;
    private WindowMessageHook _hook;

    public MainWindow()
    {
        this.InitializeComponent();
        _viewModel = new FlyoutHudViewModel();
        _inferenceEngine = new InferenceEngine();

        _viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(FlyoutHudViewModel.ResponseText))
            {
                OutputText.Text = _viewModel.ResponseText;
            }
            else if (e.PropertyName == nameof(FlyoutHudViewModel.StatusMessage))
            {
                StatusFooter.Text = $"Status: {_viewModel.StatusMessage}";
            }
        };

        IntPtr hwnd = WindowNative.GetWindowHandle(this);

        // Apply WS_EX_TOOLWINDOW to hide from Windows Taskbar and Alt+Tab menu
        int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
        SetWindowLong(hwnd, GWL_EXSTYLE, exStyle | WS_EX_TOOLWINDOW);

        // Modifier 0x0008 = Win Key. 0x4A = 'J' key. ID = 1.
        RegisterHotKey(hwnd, 1, 0x0008, 0x4A); 

        _hook = new WindowMessageHook(hwnd);
        _hook.HotKeyPressed += OnHotKeyPressed;
    }

    private async void OnHotKeyPressed(object? sender, uint hotkeyId)
    {
        if (hotkeyId == 1)
        {
            IntPtr hwnd = WindowNative.GetWindowHandle(this);
            ShowWindow(hwnd, 9);
            SetForegroundWindow(hwnd);
            this.Activate();
            System.Diagnostics.Debug.WriteLine("Win + J Pressed!");

            OutputText.Text = "Reading clipboard and analyzing...";
            string clipboardText = await ClipboardManager.GetTextAsync();
            string response = await _inferenceEngine.ProcessAsync(clipboardText);
            OutputText.Text = response;
        }
    }

    private async void ExecuteButton_Click(object? sender, RoutedEventArgs e)
    {
        await RunInferenceStreamingAsync();
    }

    private async void PromptInput_KeyDown(object? sender, KeyRoutedEventArgs e)
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
