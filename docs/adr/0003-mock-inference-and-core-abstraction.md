# 3. Mock Inference and Core Separation

We split the codebase into `MiteAI.Core`, `MiteAI.App`, and `MiteAI.Tests`. All inference orchestration is governed by `IModelInference`. A `MockInferenceService` provides deterministic responses for CI testing, rapid test execution, and headless automation, while `DirectMLInferenceService` manages production ONNX sessions with CPU fallbacks, avoiding gigabyte-scale model downloads during unit test execution and development builds.
