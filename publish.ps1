$ErrorActionPreference = 'Stop'

$projectPath = Join-Path $PSScriptRoot 'src\TextAid.App\TextAid.App.csproj'
$publishDirectory = Join-Path $PSScriptRoot 'src\TextAid.App\bin\Publish'
$nugetScratch = Join-Path $PSScriptRoot 'src\TextAid.App\obj\NuGetScratch'

New-Item -ItemType Directory -Path $publishDirectory, $nugetScratch -Force | Out-Null
if ((Get-Item -LiteralPath $publishDirectory).Attributes -band [IO.FileAttributes]::ReparsePoint) {
    throw "Publish is a link; inspect it before publishing: $publishDirectory"
}

# Preserve initialized user action data from a prior local launch while rejecting every other unexpected item.
$existingItems = @(Get-ChildItem -LiteralPath $publishDirectory -Force)
$unexpectedItems = @($existingItems | Where-Object {
    ($_.Attributes -band [IO.FileAttributes]::ReparsePoint) -or
    ($_.PSIsContainer -and $_.Name -cne 'actions')
})
if ($unexpectedItems) {
    throw "Publish contains an unexpected directory or link; inspect it before publishing: $publishDirectory"
}
foreach ($item in $existingItems) {
    if (-not $item.PSIsContainer) {
        Remove-Item -LiteralPath $item.FullName -Force
    }
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
$executable = @($publishedItems | Where-Object { -not $_.PSIsContainer -and $_.Name -ceq 'TextAid.exe' })
$unexpectedPublishedItems = @($publishedItems | Where-Object { ($_.PSIsContainer -and $_.Name -cne 'actions') -or (-not $_.PSIsContainer -and $_.Name -cne 'TextAid.exe') })
if ($executable.Count -ne 1 -or $unexpectedPublishedItems) {
    throw "Expected TextAid.exe and, only after a local launch, an actions directory in $publishDirectory."
}

Write-Output "Published: $($executable[0].FullName)"
