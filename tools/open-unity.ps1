<#
.SYNOPSIS
Opens client/ in the Unity editor that matches client/ProjectSettings/ProjectVersion.txt.

.DESCRIPTION
Finds the editor like the other tools (UNITY_EDITOR, then Unity Hub's install folders) and starts it detached.
Refuses (exit code 3) when an Editor already has the project open. Warns when Poker.Shared.dll has not been built,
because every script that uses the protocol would fail to compile.
#>
[CmdletBinding()]
param()

. "$PSScriptRoot/Common.ps1"

$clientPath = Join-Path (Get-RepoRoot) 'client'

if (Test-UnityProjectOpen)
{
	Write-Refusal 'A Unity Editor already has client/ open.'
	exit $RefusedExitCode
}

if (-not (Test-Path (Join-Path $clientPath 'Assets/Plugins/Poker.Shared/Poker.Shared.dll')))
{
	Write-Host 'Poker.Shared.dll is not built yet; run ./tools/setup.ps1 first or the project will not compile.' -ForegroundColor Yellow
}

$unity = Resolve-UnityEditor
Write-Step "Opening client/ with $unity"
Start-Process -FilePath $unity -ArgumentList @('-projectPath', "`"$clientPath`"") | Out-Null
Write-Host 'Open Assets/Scenes/Main.unity and press Play (start the server first with ./tools/server.ps1 -Background).'