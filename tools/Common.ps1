# Helpers shared by the tools/ scripts. Dot-source it: . "$PSScriptRoot/Common.ps1"
# Runs on Windows PowerShell 5.1 and on PowerShell 7 (Windows, macOS, Linux). Keep this file ASCII: 5.1 reads a file
# without a BOM in the ANSI code page.

Set-StrictMode -Version 3.0

$script:OnWindows = [System.Environment]::OSVersion.Platform -eq [System.PlatformID]::Win32NT
$script:OnMac = (-not $script:OnWindows) -and (Test-Path '/System/Library/CoreServices')

# Exit code a script uses when it refuses to run because of the environment (a locked DLL, an open Editor).
$script:RefusedExitCode = 3

function Get-RepoRoot
{
	return (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
}

function Write-Step([string]$Message)
{
	Write-Host "==> $Message" -ForegroundColor Cyan
}

function Write-Refusal([string]$Message)
{
	Write-Host $Message -ForegroundColor Yellow
}

function Get-ExecutableName([string]$Name)
{
	if ($script:OnWindows) { return "$Name.exe" }

	return $Name
}

# Finds a dotnet whose SDK satisfies global.json: PATH first, then DOTNET_ROOT, then the per-user install folders that
# dotnet-install uses. "dotnet --version" run from the repo root fails when global.json cannot be satisfied.
function Resolve-Dotnet
{
	$candidates = New-Object System.Collections.Generic.List[string]
	$onPath = Get-Command dotnet -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1
	if ($null -ne $onPath) { $candidates.Add($onPath.Source) }
	if ($env:DOTNET_ROOT) { $candidates.Add((Join-Path $env:DOTNET_ROOT (Get-ExecutableName 'dotnet'))) }
	if ($env:LOCALAPPDATA) { $candidates.Add((Join-Path $env:LOCALAPPDATA (Join-Path 'Microsoft/dotnet' (Get-ExecutableName 'dotnet')))) }
	if ($HOME) { $candidates.Add((Join-Path $HOME (Join-Path '.dotnet' (Get-ExecutableName 'dotnet')))) }

	$root = Get-RepoRoot
	foreach ($candidate in $candidates)
	{
		if (-not (Test-Path $candidate)) { continue }

		Push-Location $root
		try
		{
			$ErrorActionPreference = 'Continue'
			& $candidate --version *> $null
			$isCompatible = $LASTEXITCODE -eq 0
		}
		finally
		{
			Pop-Location
		}

		if ($isCompatible) { return $candidate }
	}

	$required = (Get-Content -Raw (Join-Path $root 'global.json') | ConvertFrom-Json).sdk.version
	throw "No .NET SDK compatible with global.json ($required or a later 8.0 feature band) was found. Install the .NET 8 SDK from https://dotnet.microsoft.com/download, or set DOTNET_ROOT to a folder that has it."
}

function Get-UnityVersion
{
	$versionFile = Join-Path (Get-RepoRoot) 'client/ProjectSettings/ProjectVersion.txt'
	foreach ($line in Get-Content $versionFile)
	{
		if ($line -match '^m_EditorVersion:\s*(\S+)') { return $Matches[1] }
	}

	throw "No m_EditorVersion in $versionFile."
}

function Get-UnityHubConfigFolder
{
	if ($script:OnWindows) { return (Join-Path $env:APPDATA 'UnityHub') }
	if ($script:OnMac) { return (Join-Path $HOME 'Library/Application Support/UnityHub') }

	return (Join-Path $HOME '.config/UnityHub')
}

function Get-UnityEditorPath([string]$EditorsFolder, [string]$Version)
{
	$versionFolder = Join-Path $EditorsFolder $Version
	if ($script:OnWindows) { return (Join-Path $versionFolder 'Editor/Unity.exe') }
	if ($script:OnMac) { return (Join-Path $versionFolder 'Unity.app/Contents/MacOS/Unity') }

	return (Join-Path $versionFolder 'Editor/Unity')
}

# Finds the editor for the version in ProjectVersion.txt: UNITY_EDITOR first, then the custom install folder chosen in
# Unity Hub, then Hub's default install folder.
function Resolve-UnityEditor
{
	$version = Get-UnityVersion
	if ($env:UNITY_EDITOR)
	{
		if (Test-Path $env:UNITY_EDITOR) { return $env:UNITY_EDITOR }

		throw "UNITY_EDITOR is set to '$env:UNITY_EDITOR', which does not exist."
	}

	$folders = New-Object System.Collections.Generic.List[string]
	$hubSetting = Join-Path (Get-UnityHubConfigFolder) 'secondaryInstallPath.json'
	if (Test-Path $hubSetting)
	{
		$custom = Get-Content -Raw $hubSetting | ConvertFrom-Json
		if ($custom) { $folders.Add([string]$custom) }
	}

	if ($script:OnWindows) { $folders.Add((Join-Path $env:ProgramFiles 'Unity/Hub/Editor')) }
	elseif ($script:OnMac) { $folders.Add('/Applications/Unity/Hub/Editor') }
	else { $folders.Add((Join-Path $HOME 'Unity/Hub/Editor')) }

	foreach ($folder in $folders)
	{
		$editor = Get-UnityEditorPath $folder $version
		if (Test-Path $editor) { return $editor }
	}

	throw "Unity $version was not found in: $($folders -join ', '). Install it through Unity Hub, or set UNITY_EDITOR to the editor executable."
}

function Get-ServerProcess
{
	# The comma keeps an empty or single-item array an array when it is returned.
	return ,@(Get-Process -Name 'Poker.Server' -ErrorAction SilentlyContinue)
}

# True when an Editor has client/ open: batch mode cannot share a project with it.
function Test-UnityProjectOpen
{
	$clientPath = Join-Path (Get-RepoRoot) 'client'
	if ($script:OnWindows)
	{
		$pattern = [regex]::Escape(($clientPath -replace '\\', '/'))
		foreach ($process in @(Get-CimInstance Win32_Process -Filter "Name='Unity.exe'" -ErrorAction SilentlyContinue))
		{
			if ($null -eq $process.CommandLine) { continue }
			if (($process.CommandLine -replace '\\', '/') -match "(?i)$pattern(/|`"|\s|$)") { return $true }
		}

		return $false
	}

	# Without command lines, an existing lock file next to a running Unity process is the best signal.
	$lockFile = Join-Path $clientPath 'Temp/UnityLockfile'
	return (Test-Path $lockFile) -and (@(Get-Process -Name 'Unity' -ErrorAction SilentlyContinue).Count -gt 0)
}

function Test-PortOpen([int]$Port)
{
	$client = New-Object System.Net.Sockets.TcpClient
	try
	{
		$connect = $client.BeginConnect('127.0.0.1', $Port, $null, $null)
		if (-not $connect.AsyncWaitHandle.WaitOne(500)) { return $false }

		$client.EndConnect($connect)
		return $true
	}
	catch
	{
		return $false
	}
	finally
	{
		$client.Close()
	}
}