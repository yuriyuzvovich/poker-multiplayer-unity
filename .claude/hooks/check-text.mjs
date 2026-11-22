// PostToolUse (Write|Edit|MultiEdit): flag CRLF, a trailing newline and em/en dashes in edited source and docs.
import { existsSync, readFileSync } from 'node:fs';

// Strip a BOM: PowerShell adds one when it pipes to a native process.
const input = JSON.parse(readFileSync(0, 'utf8').replace(/^\uFEFF/, ''));
const rawPath = input.tool_input?.file_path ?? '';
const filePath = rawPath.replace(/\\/g, '/');
// Keep in step with TextPatterns in tests/Poker.Tests/Architecture/SourceHygieneTests.cs.
if (!/\.(cs|md|asmdef|json|mjs|ps1|yml|props|csproj)$/i.test(filePath)) process.exit(0);
// Only the repo's own files follow its style; plans, memory and other files outside it do not.
const projectDir = (process.env.CLAUDE_PROJECT_DIR ?? '').replace(/\\/g, '/').replace(/\/+$/, '');
if (projectDir && !filePath.toLowerCase().startsWith(projectDir.toLowerCase() + '/')) process.exit(0);
// Unity writes these itself; third-party code is not ours to restyle.
if (/\/client\/(Packages|ProjectSettings|UserSettings|Library|Temp)\//i.test(filePath)) process.exit(0);
if (/\/Assets\/(Plugins|TextMesh Pro)\//i.test(filePath)) process.exit(0);
if (!existsSync(rawPath)) process.exit(0);

const text = readFileSync(rawPath, 'utf8');
const problems = [];
if (text.includes('\r\n')) problems.push('CRLF line endings (use LF)');
if (text.endsWith('\n')) problems.push('a trailing newline at the end of the file (remove it)');

// Built from char codes (en dash 0x2013, em dash 0x2014) so this file never contains the characters it forbids.
const dashPattern = new RegExp('[' + String.fromCharCode(0x2013, 0x2014) + ']');
const lines = text.split('\n');
const dashLines = [];
for (let i = 0; i < lines.length; i++)
{
	if (!dashPattern.test(lines[i])) continue;
	dashLines.push(i + 1);
}
if (dashLines.length > 0) problems.push('em or en dashes on line(s) ' + dashLines.slice(0, 10).join(', ') + ' (use a hyphen, comma, colon or rewrite)');

if (problems.length === 0) process.exit(0);
process.stderr.write(rawPath + ' has ' + problems.join('; ') + '. Fix it before continuing.\n');
process.exit(2);