<#
.SYNOPSIS
Builds the solution and runs the .NET tests (Poker.Shared, Poker.Server, Poker.Tests).

.DESCRIPTION
The build also copies Poker.Shared.dll into client/Assets/Plugins/Poker.Shared for Unity. A running server locks its
DLLs, so the script refuses (exit code 3) while one runs; stop it with tools/server.ps1 -Stop.

.EXAMPLE
./tools/test.ps1
./tools/test.ps1 -Filter "FullyQualifiedName~HandEngine"
#>
[CmdletBinding()]
param(
	[string]$Filter,
	[ValidateSet('quiet', 'minimal', 'normal', 'detailed')]
	[string]$Verbosity = 'quiet'
)

. "$PSScriptRoot/Common.ps1"

$servers = Get-ServerProcess
if ($servers.Count -gt 0)
{
	Write-Refusal "Poker.Server is running (process $($servers[0].Id)) and locks the DLLs the build replaces. Stop it with ./tools/server.ps1 -Stop and run again."
	exit $RefusedExitCode
}

$dotnet = Resolve-Dotnet
$arguments = @('test', (Join-Path (Get-RepoRoot) 'mp-poker.sln'), '--nologo', '--verbosity', $Verbosity)
if ($Filter) { $arguments += @('--filter', $Filter) }

& $dotnet @arguments
exit $LASTEXITCODE