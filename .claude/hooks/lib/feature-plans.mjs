// Reads the feature plans written by /plan-feature (docs/features/<name>/plan.md) for the hooks that guard them.
// Plan fields: "Status: draft|approved", "Scope: path, path", decision lines "- [ ] D<n>. ..." (open) or "- [x] ...".
import { existsSync, readdirSync, readFileSync } from 'node:fs';
import { join } from 'node:path';

export function normalize(path)
{
	return path.replace(/\\/g, '/').replace(/\/+$/, '');
}

export function projectDir()
{
	return normalize(process.env.CLAUDE_PROJECT_DIR ?? process.cwd());
}

// Plans that still need the user: not approved, or with open decisions.
export function pendingPlans(root)
{
	const featuresDir = join(root, 'docs', 'features');
	if (!existsSync(featuresDir)) return [];

	const plans = [];
	for (const entry of readdirSync(featuresDir, { withFileTypes: true }))
	{
		if (!entry.isDirectory()) continue;

		const planPath = join(featuresDir, entry.name, 'plan.md');
		if (!existsSync(planPath)) continue;

		const text = readFileSync(planPath, 'utf8');
		const status = (text.match(/^Status:\s*(\w+)/m)?.[1] ?? 'draft').toLowerCase();
		const openDecisions = [...text.matchAll(/^- \[ \] (D\d+\..*)$/gm)].map(match => match[1].trim());
		if (status === 'approved' && openDecisions.length === 0) continue;

		const scopeLine = text.match(/^Scope:\s*(.+)$/m)?.[1] ?? '';
		const scopes = scopeLine.split(',').map(part => normalize(part.trim().replace(/`/g, ''))).filter(part => part.length > 0);
		plans.push({ name: entry.name, status, openDecisions, scopes });
	}

	return plans;
}

// True when a repo-relative path falls in the plan's scope; a plan without Scope guards all production code.
export function inScope(plan, relativePath)
{
	if (plan.scopes.length === 0) return /^(shared|server|client)\//.test(relativePath);

	return plan.scopes.some(scope => relativePath === scope || relativePath.startsWith(scope + '/'));
}

// Plans, designs, docs, tests and agent setup stay editable; tests are written first.
export function isAlwaysEditable(relativePath)
{
	return /^(docs|tests|\.claude|\.github)\//.test(relativePath) || relativePath.startsWith('client/Assets/Tests/');
}

export function describe(plan)
{
	const reasons = [];
	if (plan.status !== 'approved') reasons.push('the plan is not approved (Status: ' + plan.status + ')');
	if (plan.openDecisions.length > 0) reasons.push(plan.openDecisions.length + ' open decision(s): ' + plan.openDecisions.slice(0, 3).join(' | '));
	return 'Feature "' + plan.name + '": ' + reasons.join('; ') + '. See docs/features/' + plan.name + '/plan.md.';
}

export function ask(reason)
{
	process.stdout.write(JSON.stringify({ hookSpecificOutput: { hookEventName: 'PreToolUse', permissionDecision: 'ask', permissionDecisionReason: reason } }));
}

export function readInput()
{
	// Strip a BOM: PowerShell adds one when it pipes to a native process.
	return JSON.parse(readFileSync(0, 'utf8').replace(/^﻿/, ''));
}