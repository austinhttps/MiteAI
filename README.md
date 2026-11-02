# MiteAI: Native Windows 11 On-Device AI Assistant

MiteAI is a native, air-gapped Windows 11 desktop assistant engineered in C# and .NET 10 using WinUI 3 (Windows App SDK) and LLamaSharp. It executes quantized GGUF language models directly in-process, processes clipboard queries with progressive token streaming, and augments answers using a local SQLite vector database (RAG)—all with zero telemetry, zero background daemons, and zero outbound network calls.

```
                      +-----------------------------+
                      |   Foreground Application    |
                      +--------------+--------------+
                                     |
               (Global Win + J Hotkey / Active Window Context)
                                     v
                      +-----------------------------+
                      | Context Blindspot Guard     |
                      |  (Halts on vaults/banking)  |
                      +--------------+--------------+
                                     | (Allowed sources only)
                                     v
                      +-----------------------------+
                      | Zero-Trust PII Redactor     |
                      | (Masks SSNs, Cards, Keys)   |
                      +--------------+--------------+
                                     |
                      +--------------+--------------+
                      |                             |
                      v                             v
       +----------------------------+  +----------------------------+
       | Local Knowledge Vector RAG |  | In-Process LLM (LLamaSharp)|
       | (%USERPROFILE%\Documents)  |  |  (Quantized GGUF Models)   |
       |  (SQLite Cosine Retrieval) |  +--------------+-------------+
       +--------------+-------------+                 |
                      |                               |
                      +--------------+----------------+
                                     v
                      +-----------------------------+
                      | WinUI 3 Summoning HUD       |
                      | (Real-time Token Streaming) |
                      | (Esc Instant Dismissal)     |
                      +-----------------------------+
```

---

## 1. Quickstart Guide

### Running the Standalone Executable
MiteAI publishes as a self-contained, unpackaged Windows 11 desktop application. It bundles the required native C++ libraries (`llama.dll`, `ggml.dll`, `e_sqlite3.dll`) and .NET 10 CLR dependencies into a single output folder:

1. Navigate to the publish directory:
   ```powershell
   cd src\MiteAI.App\bin\Release\net10.0-windows10.0.19041.0\win-x64\publish
   ```
2. Launch `MiteAI.App.exe`.
3. MiteAI starts silently in the background and mounts an icon into the Windows System Tray (Taskbar Notification Area).

### Building and Publishing from Source
To compile and generate the distributable self-contained build:
```powershell
# Restore, compile, and publish release binaries
dotnet publish src/MiteAI.App/MiteAI.App.csproj -c Release -r win-x64 --self-contained true
```

### Running the Automated Test Suite
Run the 63 automated xUnit tests spanning privacy redaction, context blindspots, vector search, text chunking, and HUD view models:
```powershell
dotnet test MiteAI.slnx
```

---

## 2. Summoning & Controls

MiteAI is built with `WS_EX_TOOLWINDOW` overlay behavior, completely hiding it from the Windows Taskbar and the `Alt + Tab` switcher to maintain a clutter-free workspace.

| Shortcut / Action | Behavior | Description |
| :--- | :--- | :--- |
| **`Win + J`** | **Global Summon** | Restores the HUD, activates foreground focus, and automatically reads, redacts, and analyzes the current clipboard contents with the local model. |
| **`Esc`** | **Instant Dismissal** | Cancels any ongoing inference streaming token generation and immediately hides the overlay (`SW_HIDE`) without terminating the background process. |
| **`Enter`** | **Prompt Execution** | Submits text typed into the HUD input box for on-device reasoning and live streaming. |
| **Tray Icon Right-Click** | **Context Menu** | Access quick actions: **Knowledge Base**, **Re-index Knowledge**, **Change Model...**, and **Exit MiteAI**. |

---

## 3. Model Placement & Dynamic Resolution

MiteAI runs GGUF models directly in-process via `LLamaSharp` without requiring Ollama, Docker, or external background services.

### Supported Models
- **Qwen 2.5 Coder** (e.g., `qwen2.5-coder-1.5b-instruct-q2_k.gguf`, `7b-instruct`) *(Recommended)*
- **Llama 3.2** (1B, 3B GGUF)
- **Gemma 2** (2B GGUF)
- **Mistral / Phi-3.5** (GGUF)

### Dynamic Resolution Search Order
When initialized, `InferenceEngine` automatically resolves model weights in the following order:
1. **In-Memory Override**: Active session model path.
2. **Persisted Configuration**: Path stored in `%LOCALAPPDATA%\MiteAI\settings.json`.
3. **Dedicated App Models**: `%LOCALAPPDATA%\MiteAI\models\*.gguf`.
4. **Default User Home Path**: `C:\Users\<User>\qwen2.5-coder-1.5b-instruct-q2_k.gguf`.
5. **User Models Directory**: `%USERPROFILE%\models\*.gguf`.
6. **Binary Directory**: `<AppDir>\models\*.gguf`.

### First-Run Native File Picker Fallback
If no `.gguf` file is detected in any search path when `Win + J` is pressed, MiteAI automatically initializes a native Windows 11 `FileOpenPicker` (bound to the window HWND). Once selected, the model path is saved to `%LOCALAPPDATA%\MiteAI\settings.json` and loaded into memory for instant subsequent invocations. You can change models at any time using the **Select Model...** header button or tray context menu.

---

## 4. Local Knowledge RAG Pipeline

MiteAI includes an on-device Retrieval-Augmented Generation (RAG) pipeline powered by `LocalVectorDatabase` (SQLite with cosine similarity ranking) and `TextChunkingService`.

### Adding Documents to Knowledge Base
1. Open the knowledge folder directly via the HUD header button or tray context menu (**Open Knowledge Folder**):
   ```
   %USERPROFILE%\Documents\MiteAI\Knowledge\
   ```
2. Drop in any `.txt`, `.md`, `.markdown`, or `.json` notes, documentation, or code snippets.
3. Click **Re-index Knowledge** from the tray menu (or restart the app).

### How Local Retrieval Works
1. **Pre-Persistence Redaction**: Documents are filtered through `PiiRedactionFilter` before embeddings are generated or stored on disk.
2. **Word-Boundary Chunking**: Documents are split into overlapping passages (default: 500 characters, 50-character overlap) preserving full words.
3. **SQLite Vector Persistence**: Embeddings and content are persisted to `%LOCALAPPDATA%\MiteAI\knowledge_vectors.db`.
4. **Context Augmentation**: When queries or clipboard snippets are processed, MiteAI searches the local database using cosine similarity (`topK = 3`). If matching context is found, it is dynamically prepended to the system prompt, updating the status to `Status: Augmented with Local Knowledge`.

---

## 5. Zero-Trust Privacy Specifications

MiteAI is built on a zero-trust privacy model. No network sockets are opened, and private data is scrubbed before tokenization or vector storage.

### Deterministic Regex PII Redaction
All clipboard text, manual prompts, and local documents pass through `PiiRedactionFilter` before touching the LLM or SQLite:

| Data Type | Detection Pattern | Replacement Token |
| :--- | :--- | :--- |
| **Social Security Numbers** | `\b\d{3}-\d{2}-\d{4}\b` | `[REDACTED_SSN]` |
| **Credit & Debit Cards** | Visa, MasterCard, Amex, Discover formats (`\b(?:4[0-9]{3}...)`) | `[REDACTED_CARD]` |
| **API Keys & Secrets** | OpenAI (`sk-`), GitHub (`ghp_`), AWS (`AKIA`) tokens | `[REDACTED_API_KEY]` |
| **Email Addresses** | `\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}\b` | `[REDACTED_EMAIL]` |

### Context Blindspot Inspection
Before MiteAI restores its window or touches the Windows clipboard on `Win + J`, `ContextBlindspotService` calls Win32 `GetForegroundWindow`, `GetWindowText`, and `GetWindowThreadProcessId` to inspect the focused application:

- **Excluded Vault Processes**: `1password.exe`, `bitwarden.exe`, `keepass.exe`, `lastpass.exe`, `msauthenticator.exe`, `authy.exe`.
- **Excluded Title Keywords**: `banking`, `chase online`, `wells fargo`, `bank of america`, `fidelity investments`, `paypal checkout`, `mastercard`, `visa secure`.

**Security Action**: If an excluded application is active, clipboard ingestion is **immediately halted**. The HUD alerts the user with:
```
Context suppressed: Focused application is on your privacy blocklist (password manager or financial portal). Clipboard ingestion halted to prevent secret exfiltration.
Status: Ingestion Blocked — Sensitive App Active (<ProcessName>)
```

---

## 6. Technical Specifications & Architecture

| Component | Technology | Rationale |
| :--- | :--- | :--- |
| **UI Framework** | WinUI 3 (Windows App SDK 1.6) | Native Windows 11 Fluent Design, Mica material, ~35 MB idle footprint vs 180+ MB for Electron. |
| **Inference Engine** | `LLamaSharp` 0.17.0 (CPU Backend) | Direct in-process GGUF execution without external daemons or Python runtime dependencies. |
| **Vector Database** | SQLite (`Microsoft.Data.Sqlite`) | Lightweight, single-file embedded database with cosine similarity nearest-neighbor lookup. |
| **System Tray** | `H.NotifyIcon.WinUI` | Clean WinUI 3 system tray icon and context flyout integration for taskbar-hidden apps. |
| **Global Hooking** | Win32 Subclassing (`comctl32.dll` / `user32.dll`) | Low-latency `WM_HOTKEY` message pump without polling or elevated administrative privileges. |
