<#
.SYNOPSIS
Starts or stops the local Poker.Server (ws://localhost:5080/ws by default).

.DESCRIPTION
Without switches the server runs in this console until Ctrl+C. -Background starts it detached, waits until the port
answers and writes its output to a log in the temp folder. -Stop stops every running Poker.Server. The Unity client and
the PlayMode tests connect to port 5080.

.EXAMPLE
./tools/server.ps1
./tools/server.ps1 -Background
./tools/server.ps1 -Stop
#>
[CmdletBinding(DefaultParameterSetName = 'Start')]
param(
	[Parameter(ParameterSetName = 'Start')]
	[ValidateRange(1, 65535)]
	[int]$Port = 5080,

	[Parameter(ParameterSetName = 'Start')]
	[switch]$Background,

	[Parameter(ParameterSetName = 'Stop')]
	[switch]$Stop
)

. "$PSScriptRoot/Common.ps1"

$servers = Get-ServerProcess

if ($Stop)
{
	if ($servers.Count -eq 0)
	{
		Write-Host 'No Poker.Server is running.'
		exit 0
	}

	foreach ($server in $servers)
	{
		Stop-Process -Id $server.Id -Force -Confirm:$false
		$server.WaitForExit(10000) | Out-Null
		Write-Host "Stopped Poker.Server (process $($server.Id))."
	}

	exit 0
}

if ($servers.Count -gt 0)
{
	Write-Refusal "Poker.Server is already running (process $($servers[0].Id)). Stop it with ./tools/server.ps1 -Stop."
	exit $RefusedExitCode
}

if (Test-PortOpen $Port)
{
	Write-Refusal "Port $Port is already in use by another program. Pick another one with -Port."
	exit $RefusedExitCode
}

$dotnet = Resolve-Dotnet
$project = Join-Path (Get-RepoRoot) 'server/Poker.Server'
$url = "http://localhost:$Port"

if (-not $Background)
{
	Write-Step "Starting Poker.Server on ws://localhost:$Port/ws (Ctrl+C stops it)"
	& $dotnet run --project $project -- --urls $url
	exit $LASTEXITCODE
}

$logFolder = Join-Path ([System.IO.Path]::GetTempPath()) 'mp-poker'
New-Item -ItemType Directory -Force $logFolder | Out-Null
$log = Join-Path $logFolder 'server.log'
$errorLog = Join-Path $logFolder 'server.errors.log'

# Start-Process joins the arguments with spaces without quoting them, so a path with spaces is quoted here.
$start = @{
	FilePath = $dotnet
	ArgumentList = @('run', '--project', "`"$project`"", '--', '--urls', $url)
	RedirectStandardOutput = $log
	RedirectStandardError = $errorLog
	PassThru = $true
}
if ($OnWindows) { $start['WindowStyle'] = 'Hidden' }

Write-Step "Building and starting Poker.Server in the background on ws://localhost:$Port/ws"
$runner = Start-Process @start

$deadline = [DateTime]::UtcNow.AddSeconds(120)
while (-not (Test-PortOpen $Port))
{
	if ($runner.HasExited)
	{
		Write-Host "The server stopped during startup (exit code $($runner.ExitCode)). Output:" -ForegroundColor Red
		Get-Content $log, $errorLog -Tail 30 -ErrorAction SilentlyContinue | Write-Host
		exit 1
	}

	if ([DateTime]::UtcNow -gt $deadline)
	{
		Write-Host "The server did not open port $Port within 120 s. See $log." -ForegroundColor Red
		exit 1
	}

	Start-Sleep -Milliseconds 500
}

Write-Host "Listening on ws://localhost:$Port/ws. Log: $log"
Write-Host 'Stop it with ./tools/server.ps1 -Stop.'