# Security Review

Use this guide for any skill that runs commands, uses credentials, calls external systems, installs dependencies, modifies files, or may be shared with other users.

## Trust Model

Treat skills as executable capability bundles. Even a Markdown-only skill can instruct an agent to take risky actions, and scripts can run arbitrary code.

Before using or packaging a skill, inspect enough of it to understand:

- What commands it may run.
- What files it reads or writes.
- What network services it contacts.
- What secrets or credentials it expects.
- What side effects it can cause.

## Lack Of Surprise

A skill should not do anything that would surprise a reasonable user after reading its name, description, and main instructions.

Reject or redesign skills that:

- Hide their true purpose.
- Exfiltrate data.
- Bypass authentication or permissions.
- Disable safety checks without user consent.
- Modify production systems without explicit confirmation.
- Include malware, persistence, credential theft, exploit automation, or covert monitoring.

## Tool Permissions

When a platform supports tool permissions or allowlists, grant the narrowest useful set and confirm the exact runtime semantics. Some fields pre-authorize listed tools but do not prohibit every other tool.

For Claude Code, `allowed-tools` grants permission for listed tools while the skill is active; it is not a complete sandbox. The user's normal permission settings still govern tools that are not listed.

Ask:

- Does this skill actually need shell access?
- Does it need network access?
- Can it work with read-only file access?
- Are destructive operations gated by user confirmation?
- Are external writes limited to expected systems?

## Secrets

Do not hardcode tokens, API keys, passwords, cookies, or private URLs in a skill.

Use environment variables or the platform's secret mechanism. Document required secret names without including values.

## Scripts

Review scripts for:

- Hardcoded absolute paths.
- Deleting or overwriting files without confirmation.
- Shell injection risks.
- Unpinned remote downloads.
- Silent network calls.
- Broad filesystem scans.
- Logging secrets.

Scripts should fail closed and print clear messages.

## Dependencies

Prefer standard libraries and existing project dependencies. If a dependency is necessary:

- Explain why.
- Prefer pinned versions where reproducibility matters.
- Avoid install-on-import behavior.
- Avoid downloading executable artifacts at runtime unless the user explicitly approves.

## Packaging Hygiene

Exclude:

- `.git/`
- `__pycache__/`
- `node_modules/`
- virtual environments
- logs
- eval workspaces
- temporary files
- downloaded data
- secrets
- user-specific outputs

Package only source resources needed by the skill.

## Enterprise Review

For shared or enterprise skills, add an extra review pass:

- Confirm provenance of all files.
- Verify licenses for bundled assets.
- Check for sensitive internal data.
- Run static validation.
- Run representative smoke tests.
- Record known limitations for the owner.
