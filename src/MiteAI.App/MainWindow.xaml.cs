using System;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using MiteAI.Core.Privacy;
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
    private readonly PiiRedactionFilter _privacyFilter = new();
    private WindowMessageHook _hook;

    public MainWindow()
    {
        this.InitializeComponent();
        _viewModel = new FlyoutHudViewModel();

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

        try
        {
            TrayIcon.Icon = System.Drawing.SystemIcons.Application;
        }
        catch
        {
            // Fallback if headless
        }
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
            StatusFooter.Text = "Status: Reading clipboard...";

            string clipboardText = await ClipboardManager.GetTextAsync();
            if (string.IsNullOrWhiteSpace(clipboardText))
            {
                OutputText.Text = "Clipboard is empty or contains non-text data.";
                StatusFooter.Text = "Status: Idle | No clipboard text detected";
                return;
            }

            // Zero-trust full privacy redaction
            var redaction = _privacyFilter.Redact(clipboardText);
            if (redaction.RedactionCount > 0)
            {
                var types = string.Join(", ", redaction.DetectedTypes);
                StatusFooter.Text = $"Status: Zero-Trust Redaction Active | Masked {redaction.RedactionCount} item(s) ({types})";
            }
            else
            {
                StatusFooter.Text = "Status: In-Process Inference | Streaming...";
            }

            OutputText.Text = string.Empty;

            try
            {
                await foreach (var token in InferenceEngine.StreamTextAsync(redaction.SanitizedText))
                {
                    OutputText.Text += token;
                }

                if (string.IsNullOrWhiteSpace(OutputText.Text))
                {
                    OutputText.Text = "[No response generated]";
                }

                StatusFooter.Text = redaction.RedactionCount > 0
                    ? $"Status: Complete | {redaction.RedactionCount} sensitive item(s) redacted"
                    : "Status: Complete | In-Process Inference";
            }
            catch (Exception ex)
            {
                OutputText.Text = $"Error during local inference: {ex.Message}";
                StatusFooter.Text = "Status: Inference error occurred";
            }
        }
    }

    private void ExitMenuFlyoutItem_Click(object? sender, RoutedEventArgs e)
    {
        Application.Current.Exit();
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
        string prompt = PromptInput.Text;
        if (string.IsNullOrWhiteSpace(prompt)) return;

        var redaction = _privacyFilter.Redact(prompt);
        if (redaction.RedactionCount > 0)
        {
            var types = string.Join(", ", redaction.DetectedTypes);
            StatusFooter.Text = $"Status: Zero-Trust Redaction Active | Masked {redaction.RedactionCount} item(s) ({types})";
        }
        else
        {
            StatusFooter.Text = "Status: In-Process Inference | Streaming...";
        }

        OutputText.Text = string.Empty;

        try
        {
            await foreach (var token in InferenceEngine.StreamTextAsync(redaction.SanitizedText))
            {
                OutputText.Text += token;
            }

            if (string.IsNullOrWhiteSpace(OutputText.Text))
            {
                OutputText.Text = "[No response generated]";
            }

            StatusFooter.Text = redaction.RedactionCount > 0
                ? $"Status: Complete | {redaction.RedactionCount} sensitive item(s) redacted"
                : "Status: Complete | In-Process Inference";
        }
        catch (Exception ex)
        {
            OutputText.Text = $"Error during local inference: {ex.Message}";
            StatusFooter.Text = "Status: Inference error occurred";
        }
    }
}
