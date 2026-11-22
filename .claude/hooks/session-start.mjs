// SessionStart: in cloud sessions (CLAUDE_CODE_REMOTE=true) install the .NET SDK pinned by global.json and PowerShell,
// so ./tools/*.ps1 and the tests run there too. Locally it only reminds when Poker.Shared.dll has not been built yet.
// It never fails the session: problems are reported as context and the hook exits 0.
import { spawnSync } from 'node:child_process';
import { appendFileSync, existsSync, mkdirSync } from 'node:fs';
import { homedir } from 'node:os';
import { join } from 'node:path';

const root = process.env.CLAUDE_PROJECT_DIR ?? process.cwd();
const notes = [];

function succeeds(command, args, env)
{
	const result = spawnSync(command, args, { cwd: root, env, stdio: 'ignore' });
	return result.status === 0;
}

function run(command, args, env)
{
	const result = spawnSync(command, args, { cwd: root, env, stdio: ['ignore', 'ignore', 'pipe'], encoding: 'utf8' });
	if (result.status !== 0) throw new Error(command + ' ' + args.join(' ') + ' failed: ' + (result.stderr ?? '').trim().slice(-400));
}

function prepareCloudSession()
{
	const dotnetRoot = join(homedir(), '.dotnet');
	const toolsFolder = join(dotnetRoot, 'tools');
	const env = { ...process.env, DOTNET_ROOT: dotnetRoot, PATH: dotnetRoot + ':' + toolsFolder + ':' + (process.env.PATH ?? ''), DOTNET_NOLOGO: '1', DOTNET_CLI_TELEMETRY_OPTOUT: '1' };

	// "dotnet --version" in the repo root fails unless an installed SDK satisfies global.json.
	if (!succeeds('dotnet', ['--version'], env))
	{
		mkdirSync(dotnetRoot, { recursive: true });
		const installer = join(dotnetRoot, 'dotnet-install.sh');
		run('curl', ['-sSL', 'https://dot.net/v1/dotnet-install.sh', '-o', installer], env);
		run('bash', [installer, '--jsonfile', join(root, 'global.json'), '--install-dir', dotnetRoot], env);
		notes.push('Installed the .NET SDK pinned by global.json into ~/.dotnet.');
	}

	if (!succeeds('pwsh', ['-NoProfile', '-Command', 'exit 0'], env))
	{
		run('dotnet', ['tool', 'install', '--global', 'PowerShell'], env);
		notes.push('Installed PowerShell (pwsh) as a dotnet global tool, so ./tools/*.ps1 run with "pwsh ./tools/<script>.ps1".');
	}

	if (process.env.CLAUDE_ENV_FILE)
	{
		appendFileSync(process.env.CLAUDE_ENV_FILE, 'export DOTNET_ROOT="' + dotnetRoot + '"\nexport PATH="' + dotnetRoot + ':' + toolsFolder + ':$PATH"\nexport DOTNET_NOLOGO=1\nexport DOTNET_CLI_TELEMETRY_OPTOUT=1\n');
	}

	notes.push('Cloud session: Unity is not available here; run .NET work with "pwsh ./tools/test.ps1" (or "dotnet test mp-poker.sln").');
}

try
{
	if (process.env.CLAUDE_CODE_REMOTE === 'true' && process.platform !== 'win32') prepareCloudSession();
}
catch (error)
{
	notes.push('Session setup could not finish: ' + error.message + ' Install the .NET SDK from global.json by hand.');
}

if (!existsSync(join(root, 'client', 'Assets', 'Plugins', 'Poker.Shared', 'Poker.Shared.dll')))
{
	notes.push('Poker.Shared.dll is not built yet: run ./tools/setup.ps1 (or ./tools/test.ps1) before opening Unity.');
}

if (notes.length > 0) process.stdout.write(notes.join('\n') + '\n');
process.exit(0);