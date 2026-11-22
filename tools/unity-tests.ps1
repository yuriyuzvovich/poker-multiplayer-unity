<#
.SYNOPSIS
Runs the Unity Test Runner (EditMode or PlayMode) in batch mode and prints a summary of the NUnit results.

.DESCRIPTION
Finds the editor for client/ProjectSettings/ProjectVersion.txt (see Resolve-UnityEditor in Common.ps1; UNITY_EDITOR
overrides it). Refuses (exit code 3) when an Editor already has client/ open, because batch mode cannot share the
project. PlayMode needs the server: pass -StartServer to start one for the run and stop it afterwards.

Exit codes: 0 all passed, 2 test failures, 1 the run itself failed (compile errors, crash), 3 refused.

.EXAMPLE
./tools/unity-tests.ps1 -Platform EditMode
./tools/unity-tests.ps1 -Platform EditMode -Filter PresentationQueue
./tools/unity-tests.ps1 -Platform PlayMode -StartServer
#>
[CmdletBinding()]
param(
	[Parameter(Mandatory = $true)]
	[ValidateSet('EditMode', 'PlayMode')]
	[string]$Platform,

	[string]$Filter,

	[switch]$StartServer,

	[string]$ResultsFolder = (Join-Path ([System.IO.Path]::GetTempPath()) 'mp-poker/unity-tests')
)

. "$PSScriptRoot/Common.ps1"

$root = Get-RepoRoot
$clientPath = Join-Path $root 'client'

if (Test-UnityProjectOpen)
{
	Write-Refusal 'A Unity Editor has client/ open, and batch mode cannot share the project. Close it, or run the tests in its Test Runner window (Window > General > Test Runner).'
	exit $RefusedExitCode
}

if (-not (Test-Path (Join-Path $clientPath 'Assets/Plugins/Poker.Shared/Poker.Shared.dll')))
{
	Write-Refusal 'Poker.Shared.dll is missing from the Unity project. Run ./tools/setup.ps1 (or ./tools/test.ps1) first.'
	exit $RefusedExitCode
}

$startedServer = $false
if ($Platform -eq 'PlayMode' -and (Get-ServerProcess).Count -eq 0)
{
	if (-not $StartServer)
	{
		Write-Refusal 'PlayMode tests need the server on ws://localhost:5080/ws. Start it with ./tools/server.ps1 -Background, or pass -StartServer.'
		exit $RefusedExitCode
	}

	& (Join-Path $PSScriptRoot 'server.ps1') -Background
	if ($LASTEXITCODE -ne 0) { exit 1 }
	$startedServer = $true
}

$unity = Resolve-UnityEditor
New-Item -ItemType Directory -Force $ResultsFolder | Out-Null
$results = Join-Path $ResultsFolder "$Platform-results.xml"
$log = Join-Path $ResultsFolder "$Platform-editor.log"
Remove-Item $results, $log -ErrorAction SilentlyContinue

# PlayMode UI tests need a graphics device; EditMode does not.
$arguments = @('-batchmode')
if ($Platform -eq 'EditMode') { $arguments += '-nographics' }
$arguments += @('-projectPath', "`"$clientPath`"", '-runTests', '-testPlatform', $Platform, '-testResults', "`"$results`"", '-logFile', "`"$log`"")
if ($Filter) { $arguments += @('-testFilter', "`"$Filter`"") }

Write-Step "Running $Platform tests with $unity (the first import takes a few minutes)"
try
{
	$run = Start-Process -FilePath $unity -ArgumentList $arguments -Wait -PassThru
	$unityExitCode = $run.ExitCode
}
finally
{
	if ($startedServer) { & (Join-Path $PSScriptRoot 'server.ps1') -Stop | Out-Null }
}

if (-not (Test-Path $results))
{
	Write-Host "No results file (Unity exit code $unityExitCode): the run failed before the tests. From $($log):" -ForegroundColor Red
	Select-String -Path $log -Pattern 'error CS', 'Exception', 'Aborting batchmode' -ErrorAction SilentlyContinue |
		Select-Object -First 20 |
		ForEach-Object { Write-Host "    $($_.Line.Trim())" }
	exit 1
}

[xml]$report = Get-Content -Raw $results
$summary = $report.'test-run'
Write-Host ("{0}: {1} passed, {2} failed, {3} skipped of {4}" -f $Platform, $summary.passed, $summary.failed, $summary.skipped, $summary.total)

foreach ($case in $report.SelectNodes("//test-case[@result='Failed']"))
{
	Write-Host "  FAILED $($case.fullname)" -ForegroundColor Red
	$failure = $case.SelectSingleNode('failure')
	if ($null -eq $failure) { continue }

	$message = $failure.SelectSingleNode('message')
	if ($null -ne $message) { Write-Host "    $($message.InnerText.Trim())" }
	$stack = $failure.SelectSingleNode('stack-trace')
	if ($null -eq $stack) { continue }

	$line = $stack.InnerText -split "`n" | Where-Object { $_ -match 'Assets[\\/]' } | Select-Object -First 1
	if ($line) { Write-Host "    $($line.Trim())" }
}

Write-Host "Results: $results"
if ($unityExitCode -eq 0 -or $unityExitCode -eq 2) { exit $unityExitCode }

exit 1