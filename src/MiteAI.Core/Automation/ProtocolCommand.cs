using System.Collections.Generic;

namespace MiteAI.Core.Automation;

public enum ProtocolActionType
{
    Prompt,
    Action,
    Configure
}

public record ProtocolCommand(
    ProtocolActionType Action,
    string Text,
    IReadOnlyDictionary<string, string> Parameters
);
