// PreToolUse (Write|Edit|MultiEdit): while a feature plan in docs/features/<name>/plan.md is not approved or still has
// open decisions, ask the user before editing a file inside that plan's Scope. The plan is written by /plan-feature.
import { ask, describe, inScope, isAlwaysEditable, normalize, pendingPlans, projectDir, readInput } from './lib/feature-plans.mjs';

const input = readInput();
const root = projectDir();
const filePath = normalize(input.tool_input?.file_path ?? '');
if (!filePath.toLowerCase().startsWith(root.toLowerCase() + '/')) process.exit(0);

const relative = filePath.slice(root.length + 1);
if (isAlwaysEditable(relative)) process.exit(0);

const blockers = pendingPlans(root).filter(plan => inScope(plan, relative)).map(describe);
if (blockers.length > 0)
{
	ask(blockers.join(' ') + ' Approve this edit only if you accept proceeding before those decisions are settled; otherwise resolve them with /plan-feature first.');
}

process.exit(0);