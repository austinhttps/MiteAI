# TASK: Build "Mite" - Windows 11 On-Device AI Assistant

## 1. App Features

- **Global Summoning & Flyout HUD:** A keyboard-driven shortcut (Win + J) that launches a lightweight, non-intrusive acrylic/Mica overlay with zero launch delay.
- **Active Window Contextual Assistance:** Read-only access to the foreground application title, active selection, or text buffer to offer contextual tasks (e.g., "Summarize this email," "Refactor this selected code snippet").
- **Smart Clipboard Manager:** Local semantic search and continuous parsing over clipboard history, enabling multi-format transformations (e.g., formatting raw JSON, converting table data to CSV/Markdown, or translating text on copy).
- **On-Screen Multimodal Inspection:** Quick-crop screen region capture paired with Windows AI OCR or a small vision-language model (VLM) for instant extraction of text, diagrams, error messages, and UI mockups.
- **Natural Language System Control:** Adjusts system preferences, toggles dark mode, changes audio configurations, or runs device diagnostics via direct voice or text commands.
- **Background AI Agents:** Offloads multi-step research or organizational tasks (like sorting photos or compiling documents) to background worker agents with taskbar status tracking.
- **Real-Time Voice Interaction & Live Translation:** Features hands-free wake word activation ("Hey Mite"), push-to-talk shortcuts, and live captions or audio translation across 40+ languages running locally.
- **Sandboxed System Actions:** Parsing natural-language user intent into structured commands (e.g., controlling volume, toggling Windows focus modes, managing window snapping, or organizing downloads).
- **Guarded Script Generation & Execution:** Generating PowerShell or Command Prompt scripts for system administration, requiring an explicit user confirmation modal displaying the exact script and target paths before execution.
- **Application Automation via Custom Schemes:** Launching and controlling desktop software via Windows URI schemes, CLI flags, or accessibility APIs (UI Automation).

## 2. Architecture & Tech Stack

- **Application Shell:** Build a native Windows desktop application using C# and .NET 10, utilizing WinUI 3 (Windows App SDK) for the frontend to natively support Fluent Design, Mica, and Acrylic materials.
- **Local Inference Engine:** Use `Microsoft.ML.OnnxRuntime.DirectML` to interface with local models via GPU, and provide a configurable fallback to CPU execution.
- **Local RAG & Storage:** Implement `sqlite-vec` for the local vector database and standard SQLite for configuration/clipboard history storage.
- **Mock/Dry-Run Mode:** For initial agent generation and UI testing, implement an `IModelInference` interface and create a `MockInferenceService` that returns hardcoded responses. Do not attempt to download multi-gigabyte `.gguf` or `.onnx` model weights during the Antigravity build/test phase.

## 3. Safety & Privacy Architecture

- **Explicit Action Gates & Dry-Runs:** For Guarded Script Generation and System Control, the assistant must halt and present a "Dry-Run Modal." This modal displays the exact PowerShell, CMD, or Win32 API sequence about to be executed, requiring a physical click or hotkey confirmation before applying system changes.
- **Contextual Blindspots (App Exclusions):** Allow users to define a blocklist of applications (e.g., 1Password, Bitwarden, banking web apps). When these apps are in the foreground, the assistant automatically disables Active Window Context, Screen Multimodal Inspection, and Smart Clipboard tracking.
- **On-the-Fly PII Redaction:** Implement a lightweight regex/heuristic filter over the Smart Clipboard Manager and OCR outputs to temporarily mask standard PII (Social Security Numbers, credit card formats, API keys) before writing them to the local semantic search database.
- **Process-Level Network Isolation:** To mathematically guarantee privacy, the core inference engine and background agent threads should be wrapped in a sandbox or strict Windows Firewall rule that denies all outbound network traffic, ensuring true air-gapped processing.
- **Background Agent Rate Limiting:** Establish execution boundaries for Background AI Agents. If an agent attempts to execute looping system commands or excessive file modifications (e.g., sorting 10,000 photos), it must pause at a predefined threshold and request permission to continue to prevent system resource monopolization.

## 4. Hardware Adaptation & Performance Controls

- **Dynamic NPU / GPU Routing:** Offload continuous, low-power tasks (Real-Time Voice Wake Word, audio translation, clipboard parsing) to the Copilot+ PC NPU via Windows AI APIs. Reserve the discrete GPU (DirectML) for heavy tasks like Screen OCR and deep script generation.
- **Windows Power Event Hooks:** Subscribe to `PowerSettingRegisterNotification`. When the OS switches from AC power to Battery power, automatically degrade the model tier (e.g., switch from a 3B parameter 8-bit model to a 1.5B 4-bit model) or pause background sorting agents.
- **Aggressive Memory Paging:** Implement an idle timeout (e.g., 3 minutes of inactivity). Once reached, unload the heavy language model from VRAM/RAM to disk, keeping only the tiny wake-word model active. Utilize memory mapping (`mmap`) to load weights back instantly upon the `Win + J` shortcut.
- **Context Window Throttling:** Dynamically adjust the maximum context window based on available RAM. If system memory is constrained, truncate the Active Window Context or limit clipboard history semantic search depth to prevent Out of Memory (OOM) crashes.
- **Thread Priority Management:** Assign Background AI Agents to `THREAD_PRIORITY_LOWEST` or `THREAD_PRIORITY_IDLE`. Assign the Global Summoning Flyout and Real-Time Voice Interaction to `THREAD_PRIORITY_ABOVE_NORMAL`.

## 5. Developer & Power-User Utilities

- **Local REST API & Custom URI Schemes:** Expose a localhost HTTP endpoint (e.g., `localhost:11434`) and custom Windows protocol handlers (`mite://prompt?text="..."`). This allows users to trigger the assistant via AutoHotkey, Stream Deck buttons, or custom browser extensions.
- **Bring Your Own Model (BYOM):** Provide a configuration menu allowing users to sideload their preferred local models. Support `.gguf` weights (for `llama.cpp` execution) and ONNX models, enabling users to swap the default model for specialized coding models (e.g., DeepSeek-Coder) or uncensored local weights.
- **Custom Tool-Calling Definitions:** A JSON/YAML configuration directory where power users can define their own executable scripts as "Tools." Users can map a description (e.g., "Deploys my local Docker containers") to a specific `.bat` or `.ps1` file, automatically registering it as a capability in the Natural Language System Control engine.
- **System Prompt & Persona Profiles:** A manager for hot-swappable system prompts. A user could configure a "Strict C++ Code Reviewer" profile for Visual Studio and a "Creative Copywriter" profile for Word, dynamically switching based on the Active Window Context.
- **Debug & Inference HUD:** A toggleable overlay detailing real-time inference metrics: tokens per second, VRAM utilization, active model context window, and time-to-first-token (TTFT).

## 6. Execution, Testing & Verification

- **Unit Testing:** Create an xUnit test project alongside the main application. Write tests to verify the Smart Clipboard semantic parsing, the PII Redaction regex heuristics, and the Guarded Script dry-run logic.
- **Execution Loop:** Build the application using the `.NET CLI` (`dotnet build`). Ensure all NuGet dependencies resolve cleanly.
- **Mock Verification:** Launch the application in a test harness using the `MockInferenceService` to verify that the `Win + J` Global Summoning Flyout renders correctly and responds to keyboard interrupts without requiring a live AI model.

## 7. Project Hygiene & Documentation

- **Git & Commit Strategy:** Do not output a single monolithic commit. Break the initialization into logical, atomic commits (e.g., `feat: scaffold WinUI 3 shell`, `feat: implement SQLite vector store`, `fix: enforce network isolation sandbox`).
- **Error Handling:** Avoid empty `catch (e) {}` blocks. Implement graceful degradation, structured logging for inference failures, and clear user-facing error boundaries (e.g., "GPU out of memory – falling back to CPU").
- **Comprehensive README.md:** Generate a production-grade README that goes beyond a basic feature list. It must include:
  - An architecture data flow diagram explaining how local context routes to the ONNX runtime without network telemetry.
  - Setup instructions for sideloading the MSIX package and configuring BYOM (Bring Your Own Model).
  - A "Trade-offs & Technical Choices" section explaining why WinUI 3 and DirectML were chosen over a web-wrapper like Electron.
- **CI/CD Pipeline:** Generate a `.github/workflows/build.yml` file that runs `dotnet build` and executes the xUnit test suite on every push to the `main` branch.

## 8. Artifact Generation & Resume Assets

Once the mock verification loop is complete, generate an Antigravity Artifact featuring[cite: 6]:

- A clean terminal output summary showing the successful build and test execution[cite: 6].
- A technical overview explaining how the Local RAG pipeline maintains zero-trust privacy while accessing local clipboard and window data.
- 3 high-impact, ready-to-use resume bullet points detailing the implementation of hardware-accelerated local inference (DirectML), native Windows UI (WinUI 3), and secure system integrations[cite: 6].
