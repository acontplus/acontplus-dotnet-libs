---
description: Run the CI-parity verification suite (restore, Release build, tests) and report failures.
agent: build
---

Run the verification workflow from AGENTS.md, in order, without skipping steps:

1. `dotnet restore`
2. `dotnet build acontplus-dotnet-libs.slnx --configuration Release --no-restore`
3. `dotnet test acontplus-dotnet-libs.slnx --configuration Release --no-build --verbosity normal`

If the user provided arguments, scope the run accordingly instead (e.g., a single project or test filter): $ARGUMENTS

On failure, report the root cause with the failing project/test names and propose a minimal fix. Do not modify code unless the user asks.
