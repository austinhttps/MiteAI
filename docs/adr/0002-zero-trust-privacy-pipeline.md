# 2. Zero-Trust Local Privacy Pipeline

We mandate that all contextual data ingestion (active window titles, screen OCR text, clipboard history) passes through deterministic local safety filters before persistence or vector embedding. Specifically, `ContextBlindspotService` suppresses ingestion when sensitive processes (e.g., password managers, financial apps) hold foreground focus, `PiiRedactionFilter` scrubs credentials and identifiers before writing to SQLite, and `DryRunActionGate` requires explicit operator sign-off before running system scripts.
