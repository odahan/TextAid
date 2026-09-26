$ErrorActionPreference = 'Stop'

$projectPath = Join-Path $PSScriptRoot 'src\TextAid.App\TextAid.App.csproj'
$publishDirectory = Join-Path $PSScriptRoot 'src\TextAid.App\bin\Publish'
$nugetScratch = Join-Path $PSScriptRoot 'src\TextAid.App\obj\NuGetScratch'

New-Item -ItemType Directory -Path $publishDirectory, $nugetScratch -Force | Out-Null
if ((Get-Item -LiteralPath $publishDirectory).Attributes -band [IO.FileAttributes]::ReparsePoint) {
    throw "Publish is a link; inspect it before publishing: $publishDirectory"
}

# The dedicated directory must contain only the final executable after each run.
$existingItems = @(Get-ChildItem -LiteralPath $publishDirectory -Force)
if ($existingItems | Where-Object { $_.PSIsContainer -or ($_.Attributes -band [IO.FileAttributes]::ReparsePoint) }) {
    throw "Publish contains a directory or link; inspect it before publishing: $publishDirectory"
}
foreach ($item in $existingItems) {
    Remove-Item -LiteralPath $item.FullName -Force
}

$previousNuGetScratch = $env:NUGET_SCRATCH
try {
    $env:NUGET_SCRATCH = $nugetScratch
    & dotnet publish $projectPath -c Release -r win-x64 -m:1 --self-contained true `
        -p:PublishSingleFile=true -p:PublishTrimmed=false -p:NuGetAudit=false -o $publishDirectory
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed with exit code $LASTEXITCODE."
    }
}
finally {
    if ($null -eq $previousNuGetScratch) {
        Remove-Item Env:NUGET_SCRATCH -ErrorAction SilentlyContinue
    }
    else {
        $env:NUGET_SCRATCH = $previousNuGetScratch
    }
}

$publishedItems = @(Get-ChildItem -LiteralPath $publishDirectory -Force)
if ($publishedItems.Count -ne 1 -or $publishedItems[0].PSIsContainer -or $publishedItems[0].Name -cne 'TextAid.exe') {
    throw "Expected only TextAid.exe in $publishDirectory."
}

Write-Output "Published: $($publishedItems[0].FullName)"
