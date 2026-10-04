# TextAid workspace instructions

- For every `dotnet build` and `dotnet test`, pass `-m:1` explicitly.
- In the restricted Codex workspace, set `NUGET_SCRATCH` to the writable `src/TextAid.App/obj/NuGetScratch` directory **before the first** `dotnet restore`, `dotnet build`, or `dotnet test`. Create the directory first. The default scratch location in the Windows user profile can fail with a misleading NuGet lock error under the sandbox. Do not retry the default location before using the project-local scratch path.
- In the restricted workspace, use `-p:NuGetAudit=false` when restoring, building, or testing to avoid network-only vulnerability feed warnings. This does not change package versions or the application build.
- For the final Release executable, run root `publish.ps1`; it sets the scratch path itself and writes the sole EXE to `src/TextAid.App/bin/Publish/`.
- When starting the published desktop app from Codex, use root `start.ps1`, which launches through the Windows desktop shell. Direct child-process launches can inherit the packaged host's AppData redirection and read an isolated empty configuration instead of the user's real TextAid settings. Do not copy that isolated configuration over the real user data.
