$ErrorActionPreference = 'Stop'

$executablePath = Join-Path $PSScriptRoot 'src\TextAid.App\bin\Publish\TextAid.exe'
if (-not (Test-Path -LiteralPath $executablePath -PathType Leaf)) {
    throw 'Publish TextAid with publish.ps1 before starting it.'
}

$runningInstance = Get-Process -Name TextAid -ErrorAction SilentlyContinue |
    Where-Object { $_.Path -eq $executablePath } |
    Select-Object -First 1
if ($null -ne $runningInstance) {
    Write-Output "TextAid is already running (process $($runningInstance.Id))."
    return
}

<#
<summary>Uses the desktop shell to prevent a packaged development host from redirecting TextAid's user data.</summary>
#>
$shell = New-Object -ComObject Shell.Application
$desktopHandle = 0
$desktop = $shell.Windows().FindWindowSW(0, 0, 8, [ref]$desktopHandle, 1)
if ($null -eq $desktop) {
    throw 'The Windows desktop shell is unavailable. Start TextAid manually from File Explorer.'
}

$desktop.Document.Application.ShellExecute(
    $executablePath, '', (Split-Path $executablePath), 'open', 0)
Write-Output 'TextAid launch requested through the Windows desktop shell.'
