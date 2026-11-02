using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
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

    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    private readonly FlyoutHudViewModel _viewModel;
    private readonly PiiRedactionFilter _privacyFilter = new();
    private readonly ContextBlindspotService _blindspotService = new();
    private readonly KnowledgeRagManager _ragManager = new();
    private WindowMessageHook _hook;
    private CancellationTokenSource? _inferenceCts;

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
            ModelBadge.Text = InferenceEngine.GetActiveModelName();
        }
        catch
        {
            // Fallback if headless
        }

        _ = InitializeRagAsync();
    }

    private async System.Threading.Tasks.Task InitializeRagAsync()
    {
        try
        {
            await _ragManager.InitializeAsync();
            System.Diagnostics.Debug.WriteLine($"RAG Initialized: {_ragManager.IndexedFilesCount} files, {_ragManager.IndexedChunksCount} chunks.");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to initialize RAG: {ex.Message}");
        }
    }

    private void OpenKnowledgeFolder_Click(object? sender, RoutedEventArgs e)
    {
        KnowledgeRagManager.EnsureDefaultKnowledgeDirectory();
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = KnowledgeRagManager.KnowledgeDirectory,
            UseShellExecute = true
        });
    }

    private async void ReindexKnowledge_Click(object? sender, RoutedEventArgs e)
    {
        StatusFooter.Text = "Status: Indexing Knowledge Folder...";
        int chunks = await _ragManager.SyncKnowledgeFilesAsync();
        StatusFooter.Text = $"Status: Indexed {_ragManager.IndexedFilesCount} files ({chunks} chunks)";
    }

    private async System.Threading.Tasks.Task<string?> PickModelFileAsync()
    {
        try
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.List;
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Downloads;
            picker.FileTypeFilter.Add(".gguf");

            IntPtr hwnd = WindowNative.GetWindowHandle(this);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                InferenceEngine.SetModelPath(file.Path);
                ModelBadge.Text = System.IO.Path.GetFileName(file.Path);
                return file.Path;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error launching file picker: {ex.Message}");
        }

        return null;
    }

    private async void ChangeModelMenuFlyoutItem_Click(object? sender, RoutedEventArgs e)
    {
        IntPtr hwnd = WindowNative.GetWindowHandle(this);
        ShowWindow(hwnd, 9);
        SetForegroundWindow(hwnd);
        this.Activate();

        string? selected = await PickModelFileAsync();
        if (!string.IsNullOrEmpty(selected))
        {
            OutputText.Text = $"Active model updated: {System.IO.Path.GetFileName(selected)}";
            StatusFooter.Text = $"Status: Model Loaded ({System.IO.Path.GetFileName(selected)})";
        }
    }

    private static (string WindowTitle, string ProcessName) GetForegroundWindowInfo()
    {
        IntPtr fgHwnd = GetForegroundWindow();
        if (fgHwnd == IntPtr.Zero)
        {
            return (string.Empty, string.Empty);
        }

        var sb = new StringBuilder(512);
        GetWindowText(fgHwnd, sb, sb.Capacity);
        string windowTitle = sb.ToString();

        string processName = string.Empty;
        GetWindowThreadProcessId(fgHwnd, out uint pid);
        if (pid != 0)
        {
            try
            {
                using var proc = System.Diagnostics.Process.GetProcessById((int)pid);
                processName = proc.ProcessName;
            }
            catch
            {
            }
        }

        return (windowTitle, processName);
    }

    private void HideOverlay()
    {
        try
        {
            _inferenceCts?.Cancel();
        }
        catch
        {
        }

        IntPtr hwnd = WindowNative.GetWindowHandle(this);
        ShowWindow(hwnd, 0); // SW_HIDE
    }

    private void RootGrid_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Escape)
        {
            HideOverlay();
            e.Handled = true;
        }
    }

    private async void OnHotKeyPressed(object? sender, uint hotkeyId)
    {
        if (hotkeyId == 1)
        {
            // 1. Inspect foreground window BEFORE switching focus or touching clipboard
            var (windowTitle, processName) = GetForegroundWindowInfo();
            bool isExcluded = _blindspotService.IsExcluded(windowTitle, processName);

            IntPtr hwnd = WindowNative.GetWindowHandle(this);
            ShowWindow(hwnd, 9); // SW_RESTORE
            SetForegroundWindow(hwnd);
            this.Activate();
            PromptInput.Focus(FocusState.Programmatic);
            System.Diagnostics.Debug.WriteLine($"Win + J Pressed! Foreground: '{windowTitle}' ({processName})");

            // Context Blindspot Guard: Block clipboard read if sensitive app was active
            if (isExcluded)
            {
                OutputText.Text = "Context suppressed: Focused application is on your privacy blocklist (password manager or financial portal). Clipboard ingestion halted to prevent secret exfiltration.";
                StatusFooter.Text = $"Status: Ingestion Blocked — Sensitive App Active ({processName})";
                return;
            }

            // Check if model is available, or prompt user with native picker on first run
            if (InferenceEngine.ResolveModelPath() == null)
            {
                OutputText.Text = "No local GGUF model found. Opening file picker to select a model...";
                StatusFooter.Text = "Status: Select .gguf model";

                string? selected = await PickModelFileAsync();
                if (string.IsNullOrEmpty(selected))
                {
                    OutputText.Text = "Model selection was cancelled. Place a .gguf file in %LOCALAPPDATA%\\MiteAI\\models\\ or select one to continue.";
                    StatusFooter.Text = "Status: Model Required";
                    return;
                }
            }

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

            _inferenceCts?.Cancel();
            _inferenceCts = new CancellationTokenSource();
            var ct = _inferenceCts.Token;

            // Query Local Vector RAG for relevant context
            string? ragContext = null;
            try
            {
                ragContext = await _ragManager.GetRelevantContextAsync(redaction.SanitizedText, topK: 3, cancellationToken: ct);
                if (!string.IsNullOrEmpty(ragContext))
                {
                    StatusFooter.Text = "Status: Augmented with Local Knowledge | Streaming...";
                }
            }
            catch
            {
            }

            try
            {
                await foreach (var token in InferenceEngine.StreamTextAsync(redaction.SanitizedText, ragContext, ct))
                {
                    OutputText.Text += token;
                }

                if (string.IsNullOrWhiteSpace(OutputText.Text))
                {
                    OutputText.Text = "[No response generated]";
                }

                StatusFooter.Text = redaction.RedactionCount > 0
                    ? $"Status: Complete | {redaction.RedactionCount} sensitive item(s) redacted"
                    : (!string.IsNullOrEmpty(ragContext) ? "Status: Complete | Local Knowledge RAG Augmented" : "Status: Complete | In-Process Inference");
            }
            catch (OperationCanceledException)
            {
                StatusFooter.Text = "Status: Generation dismissed";
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
        if (e.Key == Windows.System.VirtualKey.Escape)
        {
            HideOverlay();
            e.Handled = true;
            return;
        }

        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            await RunInferenceStreamingAsync();
        }
    }

    private async System.Threading.Tasks.Task RunInferenceStreamingAsync()
    {
        string prompt = PromptInput.Text;
        if (string.IsNullOrWhiteSpace(prompt)) return;

        // Check if model is available, or prompt user with native picker on first run
        if (InferenceEngine.ResolveModelPath() == null)
        {
            OutputText.Text = "No local GGUF model found. Opening file picker to select a model...";
            StatusFooter.Text = "Status: Select .gguf model";

            string? selected = await PickModelFileAsync();
            if (string.IsNullOrEmpty(selected))
            {
                OutputText.Text = "Model selection was cancelled. Place a .gguf file in %LOCALAPPDATA%\\MiteAI\\models\\ or select one to continue.";
                StatusFooter.Text = "Status: Model Required";
                return;
            }
        }

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

        _inferenceCts?.Cancel();
        _inferenceCts = new CancellationTokenSource();
        var ct = _inferenceCts.Token;

        // Query Local Vector RAG for relevant context
        string? ragContext = null;
        try
        {
            ragContext = await _ragManager.GetRelevantContextAsync(redaction.SanitizedText, topK: 3, cancellationToken: ct);
            if (!string.IsNullOrEmpty(ragContext))
            {
                StatusFooter.Text = "Status: Augmented with Local Knowledge | Streaming...";
            }
        }
        catch
        {
        }

        try
        {
            await foreach (var token in InferenceEngine.StreamTextAsync(redaction.SanitizedText, ragContext, ct))
            {
                OutputText.Text += token;
            }

            if (string.IsNullOrWhiteSpace(OutputText.Text))
            {
                OutputText.Text = "[No response generated]";
            }

            StatusFooter.Text = redaction.RedactionCount > 0
                ? $"Status: Complete | {redaction.RedactionCount} sensitive item(s) redacted"
                : (!string.IsNullOrEmpty(ragContext) ? "Status: Complete | Local Knowledge RAG Augmented" : "Status: Complete | In-Process Inference");
        }
        catch (OperationCanceledException)
        {
            StatusFooter.Text = "Status: Generation dismissed";
        }
        catch (Exception ex)
        {
            OutputText.Text = $"Error during local inference: {ex.Message}";
            StatusFooter.Text = "Status: Inference error occurred";
        }
    }
}
