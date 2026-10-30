namespace MiteAI.Core.Privacy;

public interface IContextBlindspotService
{
    bool IsExcluded(string? windowTitle, string? processName);
    void AddProcessExclusion(string processName);
    void AddTitleKeywordExclusion(string keyword);
    void RemoveProcessExclusion(string processName);
}
