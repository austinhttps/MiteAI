# 1. Native WinUI 3 and DirectML Architecture

We chose native C# and WinUI 3 (Windows App SDK) with `Microsoft.ML.OnnxRuntime.DirectML` over Electron or Chromium wrappers. Native Windows integration provides zero launch delay for the `Win + J` shortcut, immediate Mica/Acrylic Fluent composition, direct access to Win32 power notifications and window hooks, and GPU/NPU acceleration via DirectX 12 DirectML without the memory footprint of an embedded web browser or Python runtime.
