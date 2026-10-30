using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using MiteAI.Core.Actions;
using MiteAI.Core.Inference;
using MiteAI.Core.Privacy;

namespace MiteAI.Core.ViewModels;

public class FlyoutHudViewModel : INotifyPropertyChanged
{
    private readonly IModelInference _inference;
    private readonly PiiRedactionFilter _redactor;
    private readonly ContextBlindspotService _blindspots;
    private readonly DryRunActionGate _actionGate;

    private string _promptText = string.Empty;
    private string _responseText = string.Empty;
    private string _statusMessage = "Ready (Mock Inference Active)";
    private bool _isBusy = false;
    private bool _isFlyoutVisible = false;

    public bool IsFlyoutVisible
    {
        get => _isFlyoutVisible;
        set => SetField(ref _isFlyoutVisible, value);
    }

    public string PromptText
    {
        get => _promptText;
        set => SetField(ref _promptText, value);
    }

    public string ResponseText
    {
        get => _responseText;
        set => SetField(ref _responseText, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetField(ref _statusMessage, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        set => SetField(ref _isBusy, value);
    }

    public FlyoutHudViewModel(
        IModelInference? inference = null,
        PiiRedactionFilter? redactor = null,
        ContextBlindspotService? blindspots = null,
        DryRunActionGate? actionGate = null)
    {
        _inference = inference ?? new MockInferenceService();
        _redactor = redactor ?? new PiiRedactionFilter();
        _blindspots = blindspots ?? new ContextBlindspotService();
        _actionGate = actionGate ?? new DryRunActionGate();
    }

    public void OnGlobalShortcutInvoked()
    {
        // Toggle HUD on Win + J
        IsFlyoutVisible = !IsFlyoutVisible;
        StatusMessage = IsFlyoutVisible ? "Flyout Summoned (Win + J)" : "Flyout Hidden";
    }

    public async Task ExecutePromptAsync()
    {
        if (string.IsNullOrWhiteSpace(PromptText)) return;

        IsBusy = true;
        StatusMessage = "Processing on-device...";

        try
        {
            // Sanitize PII before inference
            var sanitized = _redactor.Redact(PromptText).SanitizedText;

            var result = await _inference.GenerateAsync(new InferenceRequest(sanitized));
            ResponseText = result.OutputText;
            StatusMessage = $"Completed in {result.TotalInferenceTimeMs:F1}ms ({result.TokensGenerated} tokens)";
        }
        catch (Exception ex)
        {
            ResponseText = $"Error during local execution: {ex.Message}";
            StatusMessage = "Inference halted.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
