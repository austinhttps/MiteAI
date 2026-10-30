# Mite: Windows 11 On-Device AI Assistant

Mite is a native Windows 11 desktop assistant built in C# and .NET 10. It executes local language models through DirectML, processes clipboard history and active window context, and stores embeddings in a local SQLite database without outbound network calls.

```
                      +-----------------------------+
                      |   Foreground Applications   |
                      +--------------+--------------+
                                     |
               (Window Title, Active Selection, Clipboard)
                                     v
                      +-----------------------------+
                      | Context Blindspot Inspection|
                      |  (Halts on banking/vaults)  |
                      +--------------+--------------+
                                     |
                                     v
                      +-----------------------------+
                      |    PII Redaction Engine     |
                      | (Masks SSNs, Cards, Keys)   |
                      +--------------+--------------+
                                     |
                      +--------------+--------------+
                      |                             |
                      v                             v
       +----------------------------+  +----------------------------+
       |   Local SQLite Vector DB   |  |   WinUI 3 Flyout HUD       |
       |  (sqlite-vec Embeddings)   |  |   (Win + J Global Shortcut)|
       +----------------------------+  +--------------+-------------+
                                                      |
                                                      v
                                       +----------------------------+
                                       |  Local Inference Pipeline  |
                                       | (DirectML / CPU Fallback)  |
                                       +--------------+-------------+
                                                      |
                                                      v
                                       +----------------------------+
                                       |    Dry-Run Action Gate     |
                                       | (User approval for scripts)|
                                       +----------------------------+
```

## Core Capabilities

- **Global Flyout HUD:** Press `Win + J` to open an acrylic and Mica interface with immediate focus.
- **Hardware-Accelerated Inference:** Uses `Microsoft.ML.OnnxRuntime.DirectML` for GPU execution, with CPU fallback when dedicated graphics hardware is unavailable.
- **Deterministic Privacy Filters:** Strips credentials, payment cards, and social security numbers before storing records in SQLite.
- **Context Blindspots:** Automatically disables context capture when password managers or banking applications hold system focus.
- **Guarded System Actions:** Displays script contents, target paths, and risk ratings in a dry-run confirmation modal before running system operations.
- **Smart Clipboard Transforms:** Parses clipboard text into formatted JSON or Markdown tables with integrated PII redaction.
- **Active Window Context:** Extracts foreground window title and text buffers, suppressing data automatically when private applications are active.
- **Custom URI Automation:** Supports desktop shortcuts and external automation via `mite://prompt?text="..."` handlers.
- **Persona Profiles:** Dynamic system prompt switching mapped to focused processes (such as developer tools or text editors).
- **Background Agent Coordinator:** Offloads multi-step organization tasks with progress tracking and batch modification rate limiting.
- **Power and Memory Management:** Degrades operational tiers on battery power and unloads model weights after three minutes of system idle time.

## Architecture and Data Flow

All prompt evaluations, vector searches, and context parsers operate entirely on the local device. The application process runs under local Windows firewall boundaries that deny outbound socket connections.

When clipboard data or window titles enter the pipeline:
1. `ContextBlindspotService` evaluates the executable name and window header against a security blocklist. If a match occurs, the pipeline discards the input immediately.
2. `PiiRedactionFilter` parses raw text using precompiled regex patterns to substitute sensitive tokens with explicit markers (`[REDACTED_SSN]`, `[REDACTED_CARD]`, `[REDACTED_API_KEY]`, `[REDACTED_EMAIL]`).
3. `SmartClipboardParser` detects structured payloads (JSON, CSV) and formats them into readable representations.
4. `LocalVectorDatabase` inserts sanitized text and 384-dimensional embedding vectors into SQLite tables for local cosine similarity search.
5. `DryRunActionGate` intercepts administrative commands, computes risk levels (`Low`, `Medium`, `High`, `Critical`), and issues one-time authorization tokens requiring explicit operator approval.

## Technical Choices and Trade-offs

| Decision | Selected Option | Rejected Alternative | Engineering Rationale |
| :--- | :--- | :--- | :--- |
| **Shell Framework** | WinUI 3 (Windows App SDK) | Electron / Chromium | WinUI 3 provides instant startup on `Win + J`, consumes approximately 35 MB base memory versus 180+ MB for Chromium, and supports native Windows 11 Mica backdrops and Win32 power notifications. |
| **Execution Runtime** | DirectML (`OnnxRuntime`) | PyTorch / Python IPC | DirectML targets DirectX 12 hardware across AMD, Intel, and NVIDIA GPUs without distributing a 4 GB Python distribution or CUDA binaries. |
| **Local Storage** | SQLite (`Microsoft.Data.Sqlite`) | DuckDB / Chroma | SQLite maintains minimal binary size, zero background daemon overhead, and reliable single-file persistence for vector embeddings and clipboard logs. |

## Build and Verification

### Prerequisites

- Windows 10 version 1809 (build 17763) or Windows 11
- .NET 10.0 SDK

### Running Tests

Execute the automated test suite from the repository root:

```powershell
dotnet test MiteAI.slnx
```

### Building the Application

Build the solution in Release configuration:

```powershell
dotnet build MiteAI.slnx -c Release
```

### Sideloading and BYOM Configuration

1. Enable Developer Mode in Windows Settings under **System > For developers**.
2. Install the target architecture certificate or generate a self-signed package.
3. Sideload the package:
   ```powershell
   Add-AppxPackage -Path .\bin\x64\Release\MiteAI.App.msix
   ```
4. Place custom ONNX model weights in `%LOCALAPPDATA%\MiteAI\models\`. Specify custom model filenames in `appsettings.json` under `DirectMLOptions:ModelPath`.
