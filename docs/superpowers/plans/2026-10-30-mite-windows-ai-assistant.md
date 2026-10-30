# Mite Windows 11 On-Device AI Assistant Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build "Mite", a native Windows 11 on-device AI assistant using C#/.NET 10 and WinUI 3 with DirectML local inference, zero-trust privacy redaction, context blindspots, guarded script execution gates, local SQLite vector storage, and an automated xUnit test harness.

**Architecture:** Decompose the application into a core engine (`MiteAI.Core`), a desktop shell (`MiteAI.App`), and an automated test suite (`MiteAI.Tests`). The core isolates local inference behind an `IModelInference` abstraction with mock and DirectML implementations, enforces deterministic PII scrubbing before SQLite vector ingestion, gates OS-level script execution behind explicit dry-run contracts, and manages power-aware model throttling.

**Tech Stack:** C# 13, .NET 10.0, WinUI 3 (Windows App SDK), `Microsoft.ML.OnnxRuntime.DirectML`, `Microsoft.Data.Sqlite`, xUnit, FluentAssertions.

**Spec:** `prompt.md`

## Global Constraints

- Native C#/.NET 10 WinUI 3 application with Mica/Acrylic material styling.
- Local inference via `Microsoft.ML.OnnxRuntime.DirectML` with CPU and mock fallback.
- No multi-gigabyte weight downloads during unit testing or build verification; all unit tests run against `MockInferenceService`.
- Zero-trust privacy: deterministic regex/heuristic PII redaction on clipboard/OCR data before SQLite persistence.
- Context blindspot suppression for excluded processes (e.g., password managers, banking apps).
- Explicit dry-run approval gates displaying exact scripts and paths before execution.
- Atomic git commits per task following Conventional Commits format.

## Review Focus

- Malformed PII tokens (e.g. invalid SSN lengths or non-digit chars) must not crash the redactor or bypass redaction.
- Excluded window focus checks must match process name and window title case-insensitively and handle null/empty window handles.
- Script execution must reject unconfirmed dry-runs and abort if any path outside designated sandbox limits is touched.
- Vector store similarity ranking must handle empty vectors or zero-magnitude vectors without division-by-zero exceptions.
- Power transitions (AC to Battery) must degrade model tiers and reduce context limits without dropping active user queries.

---

### Task 1: Scaffolding Solution, Core Library, and Test Suite

**Files:**
- Create: `MiteAI.sln`
- Create: `src/MiteAI.Core/MiteAI.Core.csproj`
- Create: `tests/MiteAI.Tests/MiteAI.Tests.csproj`
- Create: `tests/MiteAI.Tests/InfrastructureTests.cs`

**Interfaces:**
- Produces: Base project structure and dependencies resolving via `dotnet build`.

- [ ] **Step 1: Write the failing test**

```csharp
namespace MiteAI.Tests;

public class InfrastructureTests
{
    [Fact]
    public void BuildInfrastructure_ResolvesTargetRuntime()
    {
        Assert.True(System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/MiteAI.Tests/MiteAI.Tests.csproj`
Expected: FAIL (projects do not exist yet)

- [ ] **Step 3: Create solution and project files**

Create `MiteAI.sln`, `src/MiteAI.Core/MiteAI.Core.csproj` targeting `net10.0-windows10.0.19041.0`, and `tests/MiteAI.Tests/MiteAI.Tests.csproj` referencing `MiteAI.Core` and xUnit.

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/MiteAI.Tests/MiteAI.Tests.csproj`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add MiteAI.sln src/ tests/
git commit -m "feat: scaffold .NET 10 solution and test harness"
```

---

### Task 2: Mock Inference Service & IModelInference Abstraction

**Files:**
- Create: `src/MiteAI.Core/Inference/IModelInference.cs`
- Create: `src/MiteAI.Core/Inference/InferenceModels.cs`
- Create: `src/MiteAI.Core/Inference/MockInferenceService.cs`
- Create: `src/MiteAI.Core/Inference/DirectMLInferenceOptions.cs`
- Test: `tests/MiteAI.Tests/Inference/MockInferenceServiceTests.cs`

**Interfaces:**
- Consumes: Nothing
- Produces:
  - `Task<InferenceResult> IModelInference.GenerateAsync(InferenceRequest request, CancellationToken ct = default)`
  - `Task<float[]> IModelInference.GetEmbeddingsAsync(string text, CancellationToken ct = default)`
  - `InferenceMetrics IModelInference.GetMetrics()`

- [ ] **Step 1: Write the failing test**

```csharp
[Fact]
public async Task MockInference_ReturnsDeterministicResponseAndMetrics()
{
    var service = new MockInferenceService();
    var request = new InferenceRequest("Summarize this text", MaxTokens: 50);
    var result = await service.GenerateAsync(request);
    Assert.NotNull(result.OutputText);
    Assert.True(result.TokensGenerated > 0);
    Assert.True(service.GetMetrics().VramUsageBytes >= 0);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test --filter FullyQualifiedName~MockInferenceServiceTests`
Expected: FAIL (Types not defined)

- [ ] **Step 3: Implement `IModelInference` and `MockInferenceService`**

Implement `IModelInference`, request/response models, and `MockInferenceService` with configurable canned responses, token counting, simulated TTFT metrics, and embedding synthesis.

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test --filter FullyQualifiedName~MockInferenceServiceTests`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/MiteAI.Core/Inference tests/MiteAI.Tests/Inference
git commit -m "feat: implement IModelInference and MockInferenceService"
```

---

### Task 3: Deterministic PII Redaction Engine

**Files:**
- Create: `src/MiteAI.Core/Privacy/PiiRedactionFilter.cs`
- Create: `src/MiteAI.Core/Privacy/PiiMaskType.cs`
- Test: `tests/MiteAI.Tests/Privacy/PiiRedactionFilterTests.cs`

**Interfaces:**
- Consumes: Raw text string from clipboard or OCR
- Produces: `RedactionResult PiiRedactionFilter.Redact(string input)` returning sanitized text, masked counts, and detected token types.

- [ ] **Step 1: Write the failing test**

```csharp
[Theory]
[InlineData("My SSN is 123-45-6789 and card is 4532-1122-3344-5566", "My SSN is [REDACTED_SSN] and card is [REDACTED_CARD]")]
[InlineData("Secret key sk-proj-1234567890abcdef12345678", "Secret key [REDACTED_API_KEY]")]
public void Redact_MasksSensitiveTokensAccurately(string input, string expected)
{
    var filter = new PiiRedactionFilter();
    var result = filter.Redact(input);
    Assert.Equal(expected, result.SanitizedText);
    Assert.True(result.RedactionCount > 0);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test --filter FullyQualifiedName~PiiRedactionFilterTests`
Expected: FAIL (PiiRedactionFilter not found)

- [ ] **Step 3: Implement `PiiRedactionFilter`**

Add compiled Regex patterns for US SSN (`\b\d{3}-\d{2}-\d{4}\b`), credit card patterns, API keys (`(sk-[a-zA-Z0-9]{20,}|ghp_[a-zA-Z0-9]{20,}|AKIA[0-9A-Z]{16})`), and email patterns. Replace matched tokens with bracketed redaction tags.

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test --filter FullyQualifiedName~PiiRedactionFilterTests`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/MiteAI.Core/Privacy/PiiRedactionFilter.cs src/MiteAI.Core/Privacy/PiiMaskType.cs tests/MiteAI.Tests/Privacy/PiiRedactionFilterTests.cs
git commit -m "feat: implement deterministic PII redaction heuristics"
```

---

### Task 4: Context Blindspots & Process Exclusion Boundary

**Files:**
- Create: `src/MiteAI.Core/Privacy/ContextBlindspotService.cs`
- Create: `src/MiteAI.Core/Privacy/IContextBlindspotService.cs`
- Test: `tests/MiteAI.Tests/Privacy/ContextBlindspotServiceTests.cs`

**Interfaces:**
- Consumes: Window title string, process name string
- Produces: `bool IsExcluded(string windowTitle, string processName)` and management methods (`AddRule`, `RemoveRule`).

- [ ] **Step 1: Write the failing test**

```csharp
[Theory]
[InlineData("1Password.exe", "Vault - Personal", true)]
[InlineData("chrome.exe", "Chase Online Banking - Google Chrome", true)]
[InlineData("code.exe", "Program.cs - Visual Studio Code", false)]
public void IsExcluded_CorrectlyIdentifiesSensitiveApps(string process, string title, bool expected)
{
    var service = new ContextBlindspotService();
    Assert.Equal(expected, service.IsExcluded(title, process));
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test --filter FullyQualifiedName~ContextBlindspotServiceTests`
Expected: FAIL

- [ ] **Step 3: Implement `ContextBlindspotService`**

Implement default process exclusion list (`1password`, `bitwarden`, `keepass`, `lastpass`, `authenticator`) and window title keywords (`banking`, `chase`, `wells fargo`, `paypal`, `mastercard`). Case-insensitive matching.

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test --filter FullyQualifiedName~ContextBlindspotServiceTests`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/MiteAI.Core/Privacy/ContextBlindspot* tests/MiteAI.Tests/Privacy/ContextBlindspot*
git commit -m "feat: add contextual blindspots and process exclusions"
```

---

### Task 5: Guarded Script Generation and Dry-Run Action Gate

**Files:**
- Create: `src/MiteAI.Core/Actions/DryRunActionGate.cs`
- Create: `src/MiteAI.Core/Actions/ScriptCommand.cs`
- Create: `src/MiteAI.Core/Actions/RiskLevel.cs`
- Test: `tests/MiteAI.Tests/Actions/DryRunActionGateTests.cs`

**Interfaces:**
- Consumes: Natural language script or raw command string
- Produces: `DryRunPreview PrepareDryRun(string script, ShellType shellType)` and `ExecutionResult ExecuteConfirmed(string confirmationToken)`

- [ ] **Step 1: Write the failing test**

```csharp
[Fact]
public void PrepareDryRun_FlagsHighRiskOperations()
{
    var gate = new DryRunActionGate();
    var preview = gate.PrepareDryRun("Remove-Item -Recurse -Force C:\\Windows", ShellType.PowerShell);
    Assert.Equal(RiskLevel.Critical, preview.Risk);
    Assert.False(preview.IsApproved);
    Assert.Throws<InvalidOperationException>(() => gate.Execute(preview.ExecutionToken));
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test --filter FullyQualifiedName~DryRunActionGateTests`
Expected: FAIL

- [ ] **Step 3: Implement `DryRunActionGate`**

Build script analysis rule engine identifying recursive deletions, format drives, system directory targets, returning risk ratings (`Low`, `Medium`, `High`, `Critical`), generating a single-use approval token, and requiring explicit `ApproveToken(token)` before simulated execution.

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test --filter FullyQualifiedName~DryRunActionGateTests`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/MiteAI.Core/Actions/ tests/MiteAI.Tests/Actions/
git commit -m "feat: enforce dry-run action gates for system automation"
```

---

### Task 6: Smart Clipboard Transforms & Parsing

**Files:**
- Create: `src/MiteAI.Core/Clipboard/SmartClipboardParser.cs`
- Create: `src/MiteAI.Core/Clipboard/ClipboardFormatType.cs`
- Test: `tests/MiteAI.Tests/Clipboard/SmartClipboardParserTests.cs`

**Interfaces:**
- Consumes: Clipboard string, `PiiRedactionFilter`
- Produces: `ClipboardRecord ParseAndTransform(string rawText, string sourceApp)`

- [ ] **Step 1: Write the failing test**

```csharp
[Fact]
public void ParseAndTransform_FormatsRawJsonAndRedactsPii()
{
    var redactor = new PiiRedactionFilter();
    var parser = new SmartClipboardParser(redactor);
    var raw = "{\"apiKey\":\"sk-proj-1234567890abcdef12345678\",\"name\":\"John\"}";
    var record = parser.ParseAndTransform(raw, "code.exe");
    Assert.Equal(ClipboardFormatType.Json, record.DetectedFormat);
    Assert.Contains("[REDACTED_API_KEY]", record.FormattedContent);
    Assert.Contains("\n", record.FormattedContent); // Pretty-printed
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test --filter FullyQualifiedName~SmartClipboardParserTests`
Expected: FAIL

- [ ] **Step 3: Implement `SmartClipboardParser`**

Detect JSON, CSV, and plain text. Reformat JSON with indented formatting, convert CSV to Markdown table format, scrub sensitive tokens using `PiiRedactionFilter`, and package into `ClipboardRecord`.

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test --filter FullyQualifiedName~SmartClipboardParserTests`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/MiteAI.Core/Clipboard/ tests/MiteAI.Tests/Clipboard/
git commit -m "feat: implement smart clipboard transforms and JSON/CSV parsing"
```

---

### Task 7: Local SQLite Vector Database & Embedding Store

**Files:**
- Create: `src/MiteAI.Core/Storage/LocalVectorDatabase.cs`
- Create: `src/MiteAI.Core/Storage/VectorRecord.cs`
- Test: `tests/MiteAI.Tests/Storage/LocalVectorDatabaseTests.cs`

**Interfaces:**
- Consumes: Vector embeddings (`float[]`), metadata payload
- Produces: `Task InsertAsync(VectorRecord record)`, `Task<List<SearchResult>> SearchSimilarAsync(float[] queryVector, int limit = 5)`

- [ ] **Step 1: Write the failing test**

```csharp
[Fact]
public async Task VectorStore_InsertsAndFindsNearestNeighbor()
{
    using var db = new LocalVectorDatabase(":memory:");
    await db.InitializeAsync();
    await db.InsertAsync(new VectorRecord("doc1", "Clean code", new float[] { 1.0f, 0.0f, 0.0f }));
    await db.InsertAsync(new VectorRecord("doc2", "Machine learning", new float[] { 0.0f, 1.0f, 0.0f }));

    var results = await db.SearchSimilarAsync(new float[] { 0.9f, 0.1f, 0.0f }, limit: 1);
    Assert.Single(results);
    Assert.Equal("doc1", results[0].Id);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test --filter FullyQualifiedName~LocalVectorDatabaseTests`
Expected: FAIL

- [ ] **Step 3: Implement `LocalVectorDatabase`**

Initialize SQLite table (`records(id TEXT PRIMARY KEY, content TEXT, vector BLOB, timestamp TEXT)`). Compute cosine similarity over stored vectors in C# with support for `sqlite-vec` binary format fallback.

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test --filter FullyQualifiedName~LocalVectorDatabaseTests`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/MiteAI.Core/Storage/ tests/MiteAI.Tests/Storage/
git commit -m "feat: create local SQLite vector store with similarity search"
```

---

### Task 8: Power Adaptation & Dynamic Memory Paging Controls

**Files:**
- Create: `src/MiteAI.Core/Hardware/PowerAdaptiveManager.cs`
- Create: `src/MiteAI.Core/Hardware/PowerState.cs`
- Create: `src/MiteAI.Core/Hardware/ModelTier.cs`
- Test: `tests/MiteAI.Tests/Hardware/PowerAdaptiveManagerTests.cs`

**Interfaces:**
- Consumes: Power notifications (`PowerState.Ac`, `PowerState.Battery`), idle timer ticks
- Produces: `ModelTier CurrentTier`, `int MaxContextWindow`, `bool IsVramPagingRequested`

- [ ] **Step 1: Write the failing test**

```csharp
[Fact]
public void OnPowerStateChanged_BatteryDegradesModelTier()
{
    var manager = new PowerAdaptiveManager();
    Assert.Equal(ModelTier.HighPerformance, manager.CurrentTier);
    manager.UpdatePowerState(PowerState.Battery);
    Assert.Equal(ModelTier.PowerSaver, manager.CurrentTier);
    Assert.True(manager.MaxContextWindow < 8192);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test --filter FullyQualifiedName~PowerAdaptiveManagerTests`
Expected: FAIL

- [ ] **Step 3: Implement `PowerAdaptiveManager`**

Configure AC (`ModelTier.HighPerformance`, 8192 context) vs Battery (`ModelTier.PowerSaver`, 2048 context), track inactivity timestamps, trigger `RequestVramPaging()` when idle exceeds 3 minutes.

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test --filter FullyQualifiedName~PowerAdaptiveManagerTests`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/MiteAI.Core/Hardware/ tests/MiteAI.Tests/Hardware/
git commit -m "feat: implement hardware adaptation and power-driven memory throttling"
```

---

### Task 9: WinUI 3 Desktop Shell Scaffolding, App Manifest, and HUD

**Files:**
- Create: `src/MiteAI.App/MiteAI.App.csproj`
- Create: `src/MiteAI.App/app.manifest`
- Create: `src/MiteAI.App/App.xaml`
- Create: `src/MiteAI.App/App.xaml.cs`
- Create: `src/MiteAI.App/MainWindow.xaml`
- Create: `src/MiteAI.App/MainWindow.xaml.cs`
- Create: `src/MiteAI.App/ViewModels/FlyoutHudViewModel.cs`

**Interfaces:**
- Consumes: `MiteAI.Core` services (`IModelInference`, `ContextBlindspotService`, `PiiRedactionFilter`)
- Produces: Native Windows 11 Fluent WinUI 3 desktop shell executable with Mica backdrop.

- [ ] **Step 1: Scaffold WinUI 3 project files and XAML HUD layout**
- [ ] **Step 2: Connect `FlyoutHudViewModel` to `MockInferenceService`**
- [ ] **Step 3: Verify solution build via `dotnet build MiteAI.sln`**
- [ ] **Step 4: Commit**

```bash
git add src/MiteAI.App/
git commit -m "feat: scaffold native WinUI 3 desktop application HUD"
```

---

### Task 10: CI/CD Pipeline, Production Documentation, and Slop Review

**Files:**
- Create: `.github/workflows/build.yml`
- Create: `README.md`
- Generate Artifact: Antigravity Artifact with clean terminal execution, privacy architecture overview, and 3 resume bullet points.
- Review: Pass all documentation through `slopmonster` (`tools/deslop.py`) to enforce 5/5 score.

- [ ] **Step 1: Create GitHub Actions workflow for build and test**
- [ ] **Step 2: Generate comprehensive README.md with data flow diagrams**
- [ ] **Step 3: Generate the Antigravity Artifact in artifact directory**
- [ ] **Step 4: Run `tools/deslop.py` on README and Artifact, refining copy**
- [ ] **Step 5: Commit and finalize**
