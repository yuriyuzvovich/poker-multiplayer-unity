// PreToolUse (Bash|PowerShell): the edit hooks only see the Write and Edit tools, so a shell command that writes files
// could bypass them. This hook asks the user when a command looks like it writes a Unity .meta file under client/Assets,
// or writes inside the Scope of a feature plan that is not approved or has open decisions. It is a heuristic net under
// the rule in AGENTS.md: write source files with the edit tools, not through the shell.
import { ask, describe, inScope, isAlwaysEditable, normalize, pendingPlans, projectDir, readInput } from './lib/feature-plans.mjs';

const input = readInput();
const command = normalize(String(input.tool_input?.command ?? ''));
if (command.length === 0) process.exit(0);

// Redirections into a file (not 2>&1, not /dev/null or $null), in-place editors and file-writing commands.
const writePatterns = [
	/(^|[^0-9&>])>{1,2}\s*(?!&|\/dev\/null|\$null)\S/,
	/\bsed\s+(-[a-zA-Z]*i|--in-place)/,
	/\bperl\s+-[a-zA-Z]*i/,
	/\btee\b/,
	/\b(Set-Content|Add-Content|Out-File|New-Item|Copy-Item|Move-Item|Rename-Item)\b/i,
	/(^|[;&|]\s*|\s)(cp|mv|install)\s/,
	/\bgit\s+(mv|checkout\s+--|restore)\b/,
	/(WriteAllText|WriteAllBytes|AppendAllText|writeFileSync|appendFileSync|copyFileSync|renameSync)/,
];
if (!writePatterns.some(pattern => pattern.test(command))) process.exit(0);

const reasons = [];
if (/client\/Assets\/\S*\.meta\b/i.test(command) || /\.meta\b/.test(command) && /client\/Assets/i.test(command))
{
	reasons.push('This command seems to write .meta files under client/Assets. Unity owns them; moving an asset together with its .meta (git mv) is fine, writing one by hand breaks GUIDs.');
}

const root = projectDir();
// Repo-relative paths, whatever prefix the command used (relative, ./, E:/..., /e/... in Git Bash).
const mentioned = [...command.matchAll(/(?:^|[\s"'=(/])((?:shared\/Poker\.Shared|server\/Poker\.Server|client\/Assets)(?:\/[\w.-]+)*)/g)]
	.map(match => match[1])
	.filter(path => !isAlwaysEditable(path));
const blockers = new Set();
for (const plan of pendingPlans(root))
{
	if (mentioned.some(path => inScope(plan, path))) blockers.add(describe(plan));
}

if (blockers.size > 0) reasons.push(...blockers, 'Prefer the edit tools for source files; approve only if this write is intended before the plan is settled.');
if (reasons.length > 0) ask(reasons.join(' '));

process.exit(0);