using System;
using System.Collections.Generic;

namespace MiteAI.Core.Automation;

public class MiteProtocolParser
{
    public ProtocolCommand? Parse(string uriString)
    {
        if (string.IsNullOrWhiteSpace(uriString)) return null;

        if (!Uri.TryCreate(uriString, UriKind.Absolute, out var uri)) return null;

        if (!string.Equals(uri.Scheme, "mite", StringComparison.OrdinalIgnoreCase)) return null;

        var action = uri.Host.ToLowerInvariant() switch
        {
            "prompt" => ProtocolActionType.Prompt,
            "action" => ProtocolActionType.Action,
            "configure" => ProtocolActionType.Configure,
            _ => ProtocolActionType.Prompt
        };

        var query = uri.Query.TrimStart('?');
        var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(query))
        {
            var pairs = query.Split('&', StringSplitOptions.RemoveEmptyEntries);
            foreach (var pair in pairs)
            {
                var kvp = pair.Split('=', 2);
                var key = Uri.UnescapeDataString(kvp[0]);
                var val = kvp.Length > 1 ? Uri.UnescapeDataString(kvp[1]) : string.Empty;
                parameters[key] = val;
            }
        }

        parameters.TryGetValue("text", out var textValue);
        textValue ??= string.Empty;

        return new ProtocolCommand(action, textValue, parameters);
    }
}
