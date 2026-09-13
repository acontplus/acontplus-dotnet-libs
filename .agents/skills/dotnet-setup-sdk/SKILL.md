---
name: dotnet-setup-sdk
description: >-
  Set up the correct .NET SDK version locally. Use when a build fails due to
  a missing or wrong SDK, or when global.json specifies a version that is not
  installed. This skill detects the required version from global.json and
  installs it using the official dotnet-install script.
---

# Setup Local .NET SDK

## When to Use

Activate this skill **before building or running any .NET project** if you
suspect the required SDK version might not be installed. Typical triggers:

* `dotnet build` fails with *"The SDK 'Microsoft.NET.Sdk' specified could not
  be found"* or a version-mismatch error.
* The repository contains a `global.json` pinning an SDK version you haven't
  installed.
* You are setting up a fresh development environment.

## Steps

### 1. Determine the Required SDK Version

```bash
cat global.json
```

Look for the `sdk.version` field or verify .NET 10 is installed for `net10.0` targets:

```json
{
  "sdk": {
    "version": "10.0.100",
    "rollForward": "latestFeature"
  }
}
```
Acontplus .NET Libraries target `net10.0` across all libraries in `src/` and `apps/src/`.

### 2. Check Whether That Version Is Already Installed

```bash
dotnet --list-sdks
```

If the output already contains the required version (or a compatible one given
the `rollForward` policy), **stop here – no action is needed**.

### 3. Install the Missing SDK

#### Linux / macOS

```bash
curl -sSL https://dot.net/v1/dotnet-install.sh -o dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --version "$(jq -r '.sdk.version' global.json)"
```

> **Tip:** If `jq` is not available, read the version from `global.json`
> manually and pass it as a literal string.

#### Windows (PowerShell)

```powershell
Invoke-WebRequest 'https://dot.net/v1/dotnet-install.ps1' -OutFile 'dotnet-install.ps1'
$version = (Get-Content global.json | ConvertFrom-Json).sdk.version
./dotnet-install.ps1 -Version $version
```

### 4. Verify

```bash
dotnet --version        # should print the installed version
dotnet --list-sdks      # should list the new SDK
```

### 5. Optional: Add to PATH

If `dotnet` is not found after installation, add the install location to
`PATH`:

```bash
export DOTNET_ROOT=$HOME/.dotnet
export PATH=$PATH:$DOTNET_ROOT
```
