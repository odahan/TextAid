# TextAid

<p align="center">
  <img src="assets/Logo-final.png" alt="TextAid logo" width="480">
</p>

**Local-first text transformer & translator for Windows.**

TextAid captures selected text from another Windows application, applies one configurable AI transformation, shows the result, and lets the user replace the original selection, copy the result, or cancel. It uses a local Ollama model by default; a remote provider is an explicit user choice.

## Features

- Capture selected text with `Ctrl+C`, then `C` to choose an action, or `Ctrl+C`, then `T` for immediate translation.
- Rewrite, correct, translate, summarize, shorten, expand, simplify, change tone, humanize, or create declarative custom actions.
- Preview raw or rendered Markdown results in a dark Windows interface.
- Keep model traffic local by default, with clear configuration for on-premises or external providers.
- Run as a self-contained Windows x64 executable; no .NET runtime is required for the published application.

## Requirements

- Windows x64
- .NET SDK 10 for building from source
- [Ollama](https://ollama.com/) and a compatible local model for the default configuration

## Build and test

In PowerShell, from the repository root:

```powershell
$env:NUGET_SCRATCH = Join-Path (Get-Location) 'src\TextAid.App\obj\NuGetScratch'
New-Item -ItemType Directory -Path $env:NUGET_SCRATCH -Force | Out-Null
dotnet restore TextAid.sln -m:1 -p:NuGetAudit=false
dotnet build TextAid.sln -m:1 -p:NuGetAudit=false
dotnet test TextAid.sln -m:1 -p:NuGetAudit=false
```

To create the canonical single-file Release executable, run:

```powershell
.\publish.ps1
```

The resulting executable is `src/TextAid.App/bin/Publish/TextAid.exe`.

## Project method

This repository is maintained with **SAW 3.2 (SDD Another Way)**, a method created by Olivier Dahan © 2025–2026 that carries forward Pro-Spec 3. The method is Markdown- and Git-native: it does not depend on an external tool.

The product intent, enduring rules, decisions, project status, and historical specifications remain in the repository. Before changing a planned lot or its validation state, follow the entry procedure in [the SAW 3.2 / Pro-Spec 3 reference](docs/PROSPEC-3-SPECIFICATION.md). French source copies are retained beside the translated documents with the `.FR.md` suffix.

## Documentation

- [Product intent](PROJECT.md)
- [Current project status](STATUS.md)
- [Language-pack layout](docs/language-packs.md)
- [Source specification map](docs/SOURCE-MAP.md)
- [SAW 3.2 / Pro-Spec 3 reference](docs/PROSPEC-3-SPECIFICATION.md)

## License and attribution

TextAid is source-available under the [Creative Commons Attribution-NonCommercial 4.0 International License](LICENSE). You may copy, share, and adapt it for non-commercial purposes only. Every distribution must retain the attribution **Olivier Dahan © 2026** and the contact `odahan [at] e-naxos [dot] com`, indicate changes, and include the license notice. Commercial use requires the author's prior written agreement.

This is intentionally a non-commercial license and therefore is not an OSI-approved Open Source license.
