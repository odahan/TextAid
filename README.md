# TextAid

[Français](README-FR.md)

![TextAid logo](assets/Logo-final.png)

**Local-first text transformer & translator for Windows.**

TextAid captures selected text from another Windows application, applies one configurable AI transformation, shows the result, and lets the user replace the original selection, copy the result, or cancel. It uses a local Ollama model by default; a remote provider is an explicit user choice.

## App preview

![TextAid translating selected text](assets/Screenshots/02%20Call%20TextAid%20to%20translate.png)

## Features

- Capture selected text with `Ctrl+C`, then `C` to choose an action, or `Ctrl+C`, then `T` for immediate translation.
- Identify the captured language through an asynchronous AI request on both shortcuts, without blocking the interface. Reuse the response until the text changes; ambiguous input requires an explicit translation destination.
- Rewrite, correct, translate, summarize, shorten, expand, simplify, change tone, humanize, or create declarative custom actions.
- Preview raw or rendered Markdown results in a dark Windows interface.
- In the result window, use `Ctrl+Shift+C` to copy the full result or `Ctrl+Enter` to replace the original selection. Actions run when the shortcut keys are released and a valid result is available; replacement also requires a captured source window. `Ctrl+C` keeps its usual text-selection behavior.
- Keep model traffic local by default, with clear configuration for on-premises or external providers.
- Run as a self-contained Windows x64 executable; no .NET runtime is required for the published application.

## Local model startup and action settings

TextAid preloads the enabled, configured local Ollama model in the background 30 seconds after startup. If Ollama is not available, it retries availability after another 30 seconds, with each probe limited to five seconds. Preload stops after success and is cancelled when TextAid exits or settings are saved. A settings save schedules a new delayed attempt. An already loaded model is left in place. TextAid does not override the server's keep-alive policy; set `OLLAMA_KEEP_ALIVE=-1` in the Ollama server environment to retain models indefinitely.

Every action, including Translate, defaults to Thinking Off. The Actions editor supports Off, Low, Medium, and High, plus an optional context override in tokens. An empty context inherits the model profile; use 16384 in the local profile for a shared 16K baseline. Existing action JSON files without these fields use Off and inherit context. The JSON fields are `thinking` (for example, `"Off"`) and `contextSizeOverride` (an integer of at least 512, or null). Translate retains these inference choices while its system prompt and routing remain protected. Switching context between actions or apps can cause Ollama to reload a runner.

Safe Debug records preload events, requested and loaded context, model and GPU memory allocation, GPU memory share, and the retention expiry as Unix seconds, alongside inference timing metrics. GPU memory share describes allocation, not live GPU utilization. No diagnostic log is created with Debug off.

Shortcut detection verifies Windows modifier state at both sequence keys and clears stale observed modifiers. Keep Ctrl held throughout `Ctrl+C+C` or `Ctrl+C+T`; releasing or changing a modifier cancels the pending sequence. Software-injected keyboard input cannot activate a shortcut. With Debug enabled, `TextAid.debug.log` in `%LOCALAPPDATA%\TextAid` records `shortcut-sequence-started`, `shortcut-activated`, `shortcut-modifiers-resynchronized`, and injected input rejected during a pending sequence. These targeted events include virtual key, scan code, hook flags, native input timestamp, left/right Ctrl state, and observed/Windows/verified modifier masks (Ctrl=1, Shift=2, Alt=4, Win=8). Full log is not required, and ordinary typing is not continuously logged. Use **Open debug-log folder** in Settings to find the file; each new debug session replaces it.

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

To launch the published app from a packaged development host such as Codex, use `./start.ps1`. It dispatches through the Windows desktop shell so TextAid uses the normal user configuration. You can also start the executable directly from File Explorer.

## Project method

This repository is maintained with **SAW 3.2 (SDD Another Way)**, a method created by Olivier Dahan © 2025–2026 that carries forward Pro-Spec 3. The method is Markdown- and Git-native: it does not depend on an external tool.

The product intent, enduring rules, decisions, project status, and historical specifications remain in the repository. Before changing a planned lot or its validation state, follow the entry procedure in [the SAW 3.2 / Pro-Spec 3 reference](docs/PROSPEC-3-SPECIFICATION.md). French source copies are retained beside the translated documents with the `.FR.md` suffix.

## AI-centric bootstrap

To conduct this project with an AI, use this prompt:

> **Read the `README.md` file and execute the next step.**

The active bootstrap is deliberately contained in this README. An AI must first read the product intent in `PROJECT.md`, enduring constraints in `RULES.md`, all active decisions in `LEDGER.md` (and any referenced obsolete decisions), and the current plan in `STATUS.md`. It must then read the active lot's `SPEC`, `FINDINGS`, `GATES`, and, where present, `CONVERGENCE`. Consult `HISTORY.md` when chronology helps recovery or audit.

`STATUS.md` is the sole authority for lot state. It identifies the next `Planned` lot and its dependencies; only one lot may be `In-progress`, `Ready-to-close`, or `Blocked` at a time. A human decision is required to move a lot to `In-progress`, to close it, to validate a HUMAN gate, to accept a deviation, or to make another significant change of meaning.

While a lot is active, implement its `SPEC`, record knowledge in `FINDINGS`, keep the result location, remaining work, blockers, and next action recoverable through `STATUS.md`, and append significant operations to `HISTORY.md`. Evaluate every gate against an identifiable result. A lot may close only when every applicable gate is validly `PASS` or `N/A`, all requirements and findings have been accounted for, and a human accepts the closure. Update `STATUS.md` last.

> If you need an AI powerful enough to manage your project, and the project is large enough to require a method, then the AI being used is sufficient to manage all of the method's bureaucracy.

The complete historical bootstrap is preserved unchanged in [docs/README.BOOTSTRAP.md](docs/README.BOOTSTRAP.md). The active instructions above supersede only its former references to an external Pro-Spec tool, which SAW 3.2 no longer uses.

## Method validation

TextAid was built both for its own purpose and to put SAW to the test. Its development used only GPT-5.6 Terra with Medium reasoning effort and an OpenAI USD 20 subscription. The project demonstrates that Microsoft’s Spec-Driven Development (SDD) can be adapted into a practical, fully AI-centric method. SAW (SDD Another Way) is that adaptation: an alternative to the support provided by Microsoft’s Spec-Kit for applying SDD in a real project.

## Documentation

- [Product intent](PROJECT.md)
- [Current project status](STATUS.md)
- [Language-pack layout](docs/language-packs.md)
- [Source specification map](docs/SOURCE-MAP.md)
- [SAW 3.2 / Pro-Spec 3 reference](docs/PROSPEC-3-SPECIFICATION.md)
- [Original bootstrap README](docs/README.BOOTSTRAP.md)

## License and attribution

TextAid is source-available under the [Creative Commons Attribution-NonCommercial 4.0 International License](license.md). You may copy, share, and adapt it for non-commercial purposes only. Every distribution must retain the attribution **Olivier Dahan © 2026** and the contact `odahan [at] e-naxos [dot] com`, indicate changes, and include the license notice. Commercial use requires the author's prior written agreement.

This is intentionally a non-commercial license and therefore is not an OSI-approved Open Source license.
