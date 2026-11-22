<#
.SYNOPSIS
Prepares a fresh clone: checks the .NET SDK and Unity, and builds Poker.Shared into the Unity project.

.DESCRIPTION
Poker.Shared.dll is not committed (its .meta is). Unity needs it in client/Assets/Plugins/Poker.Shared before the
project first opens, or every script that uses the protocol fails to compile.
#>
[CmdletBinding()]
param()

. "$PSScriptRoot/Common.ps1"

$root = Get-RepoRoot

Write-Step 'Looking for a .NET SDK that matches global.json'
$dotnet = Resolve-Dotnet
Push-Location $root
try { $sdk = & $dotnet --version } finally { Pop-Location }
Write-Host "    $dotnet (SDK $sdk)"

Write-Step 'Building Poker.Shared and copying it into the Unity project'
& $dotnet build (Join-Path $root 'shared/Poker.Shared/Poker.Shared.csproj') --nologo --verbosity quiet
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$dll = Join-Path $root 'client/Assets/Plugins/Poker.Shared/Poker.Shared.dll'
if (-not (Test-Path $dll)) { throw "The build succeeded but $dll is missing." }
Write-Host "    $dll"

Write-Step "Looking for Unity $(Get-UnityVersion)"
try
{
	Write-Host "    $(Resolve-UnityEditor)"
}
catch
{
	Write-Host "    $($_.Exception.Message)" -ForegroundColor Yellow
	Write-Host '    The server and the .NET tests work without Unity.'
}

Write-Host ''
Write-Host 'Next:'
Write-Host '  ./tools/server.ps1        start the server on ws://localhost:5080/ws'
Write-Host '  ./tools/test.ps1          run the .NET tests'
Write-Host '  Open client/ in Unity Hub, open Assets/Scenes/Main.unity and press Play.'