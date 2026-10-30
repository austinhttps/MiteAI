using System;
using System.Collections.Generic;
using System.IO;

namespace MiteAI.Core.Personas;

public class PersonaManager
{
    private readonly Dictionary<string, PersonaProfile> _personas = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _processMappings = new(StringComparer.OrdinalIgnoreCase);
    private string? _explicitOverridePersonaName;

    public static readonly PersonaProfile DefaultPersona = new(
        Name: "General Assistant",
        Description: "Helpful, concise desktop AI assistant.",
        SystemPrompt: "You are Mite, a privacy-first on-device Windows 11 assistant. Provide direct, accurate assistance."
    );

    public PersonaManager()
    {
        RegisterPersona(DefaultPersona);

        RegisterPersona(new PersonaProfile(
            Name: "Strict C++ Code Reviewer",
            Description: "Specialized in Modern C++, RAII, memory safety, and performance.",
            SystemPrompt: "You are a senior C++ engineer. Review code strictly for memory safety, undefined behavior, and optimal resource management."
        ));

        RegisterPersona(new PersonaProfile(
            Name: "Creative Copywriter",
            Description: "Focuses on engaging narrative, persuasive clarity, and structure.",
            SystemPrompt: "You are an expert copywriter. Enhance clarity, tone, and flow while eliminating unnecessary phrasing."
        ));

        // Default application process mappings
        MapProcessToPersona("devenv.exe", "Strict C++ Code Reviewer");
        MapProcessToPersona("code.exe", "Strict C++ Code Reviewer");
        MapProcessToPersona("winword.exe", "Creative Copywriter");
        MapProcessToPersona("notepad.exe", "General Assistant");
    }

    public void RegisterPersona(PersonaProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        _personas[profile.Name] = profile;
    }

    public void MapProcessToPersona(string processName, string personaName)
    {
        var cleanName = Path.GetFileName(processName);
        _processMappings[cleanName] = personaName;
    }

    public void SetActiveOverride(string personaName)
    {
        if (_personas.ContainsKey(personaName))
        {
            _explicitOverridePersonaName = personaName;
        }
    }

    public void ClearActiveOverride()
    {
        _explicitOverridePersonaName = null;
    }

    public PersonaProfile ResolvePersonaForProcess(string? processName)
    {
        if (!string.IsNullOrEmpty(_explicitOverridePersonaName) &&
            _personas.TryGetValue(_explicitOverridePersonaName, out var overridden))
        {
            return overridden;
        }

        if (!string.IsNullOrWhiteSpace(processName))
        {
            var fileName = Path.GetFileName(processName);
            if (_processMappings.TryGetValue(fileName, out var personaName) &&
                _personas.TryGetValue(personaName, out var mapped))
            {
                return mapped;
            }
        }

        return DefaultPersona;
    }
}
