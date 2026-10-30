# Mite AI Assistant

Native Windows 11 On-Device AI Assistant running fully local, privacy-first inference with DirectML and WinUI 3.

## Language

**Flyout HUD**:
The ephemeral keyboard-summoned (Win + J) non-intrusive Mica/Acrylic overlay interface providing prompt execution and context actions.
_Avoid_: Popup, window, dialog

**Context Blindspot**:
A security boundary that automatically halts screen reading, active window inspection, and clipboard ingestion when excluded sensitive applications (e.g., password managers, banking browsers) are focused.
_Avoid_: Blacklist, app ignore, filter

**Dry-Run Gate**:
An explicit verification barrier displaying the exact PowerShell, CMD, or Win32 API commands and target file paths requiring human approval before system execution.
_Avoid_: Confirmation dialog, alert prompt

**PII Redactor**:
A deterministic heuristic and regular expression filter that masks sensitive identifiers (SSNs, credit card numbers, secret keys) prior to vector database ingestion.
_Avoid_: Data cleanser, sanitizer

**Inference Engine**:
The local hardware-accelerated ONNX execution pipeline utilizing DirectML with CPU fallback and mock modes.
_Avoid_: AI backend, cloud API

**Clipboard Record**:
A structured data item holding captured clipboard text, timestamp, source window handle, and scrubbed embedding vector for semantic search.
_Avoid_: History snippet, clipboard row
