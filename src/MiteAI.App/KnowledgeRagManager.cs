using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MiteAI.Core.Inference;
using MiteAI.Core.Privacy;
using MiteAI.Core.RAG;
using MiteAI.Core.Storage;

namespace MiteAI.App;

public class KnowledgeRagManager : IDisposable
{
    private static readonly string AppDataDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "MiteAI");
    private static readonly string DatabasePath = Path.Combine(AppDataDir, "knowledge_vectors.db");

    public static string KnowledgeDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "MiteAI", "Knowledge");

    private readonly LocalVectorDatabase _vectorDatabase;
    private readonly DirectMLInferenceService _embeddingEngine;
    private readonly PiiRedactionFilter _redactionFilter;
    private readonly LocalRagService _ragService;
    private bool _isInitialized = false;

    public int IndexedChunksCount { get; private set; } = 0;
    public int IndexedFilesCount { get; private set; } = 0;

    public KnowledgeRagManager()
    {
        Directory.CreateDirectory(AppDataDir);
        _vectorDatabase = new LocalVectorDatabase(DatabasePath);
        _embeddingEngine = new DirectMLInferenceService(new DirectMLInferenceOptions { FallbackToCpu = true });
        _redactionFilter = new PiiRedactionFilter();
        _ragService = new LocalRagService(_vectorDatabase, _embeddingEngine, _redactionFilter);
    }

    public async Task InitializeAsync()
    {
        if (_isInitialized) return;

        await _vectorDatabase.InitializeAsync();
        EnsureDefaultKnowledgeDirectory();
        await SyncKnowledgeFilesAsync();
        _isInitialized = true;
    }

    public static void EnsureDefaultKnowledgeDirectory()
    {
        if (!Directory.Exists(KnowledgeDirectory))
        {
            Directory.CreateDirectory(KnowledgeDirectory);

            string sampleFile = Path.Combine(KnowledgeDirectory, "mite_overview.md");
            if (!File.Exists(sampleFile))
            {
                File.WriteAllText(sampleFile,
@"# MiteAI Local Assistant Overview
MiteAI is a native Windows 11 on-device AI assistant engineered for zero-trust privacy and sub-second shortcut summoning.
Core Features:
- Summon HUD anywhere via global hotkey: Win + J
- In-process local LLM execution using LLamaSharp (Qwen 2.5 Coder)
- Zero-trust PII redaction for passwords, API tokens, credit cards, SSNs, and emails
- Privacy blindspots for password managers (1Password, Bitwarden, KeePass) and banking portals
- Local vector database (SQLite) for knowledge search
- Completely air-gapped without telemetry or external cloud dependencies
");
            }
        }
    }

    public async Task<int> SyncKnowledgeFilesAsync(CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(KnowledgeDirectory))
        {
            return 0;
        }

        string[] searchPatterns = ["*.txt", "*.md", "*.markdown", "*.json"];
        var files = new List<string>();
        foreach (var pattern in searchPatterns)
        {
            files.AddRange(Directory.GetFiles(KnowledgeDirectory, pattern, SearchOption.TopDirectoryOnly));
        }

        int totalChunks = 0;
        int fileCount = 0;

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                int chunks = await _ragService.IngestFileAsync(file, cancellationToken);
                totalChunks += chunks;
                fileCount++;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to ingest {file}: {ex.Message}");
            }
        }

        IndexedChunksCount = totalChunks;
        IndexedFilesCount = fileCount;
        return totalChunks;
    }

    public async Task<string?> GetRelevantContextAsync(string query, int topK = 3, float minScore = 0.35f, CancellationToken cancellationToken = default)
    {
        if (!_isInitialized || string.IsNullOrWhiteSpace(query))
        {
            return null;
        }

        try
        {
            var results = await _ragService.QueryAsync(query, topK, cancellationToken);
            var relevant = results.Where(r => r.RelevanceScore >= minScore).ToList();

            if (relevant.Count == 0)
            {
                return null;
            }

            var sb = new StringBuilder();
            sb.AppendLine("[Retrieved Local Knowledge Context]");
            foreach (var match in relevant)
            {
                sb.AppendLine($"--- Source: {match.DocumentId} (Relevance: {match.RelevanceScore:F2}) ---");
                sb.AppendLine(match.Content);
            }

            return sb.ToString().Trim();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"RAG query error: {ex.Message}");
            return null;
        }
    }

    public void Dispose()
    {
        _vectorDatabase.Dispose();
        _embeddingEngine.Dispose();
    }
}
