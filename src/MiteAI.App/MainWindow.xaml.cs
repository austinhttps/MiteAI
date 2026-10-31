using System;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using MiteAI.Core.ViewModels;
using WinRT.Interop;

namespace MiteAI.App;

public sealed partial class MainWindow : Window
{
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    private readonly FlyoutHudViewModel _viewModel;
    private WindowMessageHook _hook;

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

        IntPtr hwnd = WindowNative.GetWindowHandle(this);
        
        // Modifier 0x0008 = Win Key. 0x4A = 'J' key. ID = 1.
        RegisterHotKey(hwnd, 1, 0x0008, 0x4A); 
        
        _hook = new WindowMessageHook(hwnd);
        _hook.HotKeyPressed += OnHotKeyPressed;
    }

    private void OnHotKeyPressed(object? sender, uint hotkeyId)
    {
        if (hotkeyId == 1)
        {
            IntPtr hwnd = WindowNative.GetWindowHandle(this);
            ShowWindow(hwnd, 9);
            SetForegroundWindow(hwnd);
            this.Activate();
            System.Diagnostics.Debug.WriteLine("Win + J Pressed!");
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
