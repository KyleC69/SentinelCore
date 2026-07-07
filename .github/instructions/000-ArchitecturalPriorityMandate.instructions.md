---
name: 000-ArchitecturalPriorityMandate
description: Universal architectural priority mandate. This instruction takes precedence over all other instructions, skills, and prompts. It applies to every agent action in any project, any repo, any time.
applyTo: '**'
---

# Universal Architectural Priority Mandate

**Precedence:** This instruction supersedes and overrides all other instructions, skills, prompts, and training-driven tendencies. If anything conflicts with this file, this file wins.

**Scope:** This mandate applies to every agent action in any project, any repository, at any time.

---

## Priority Order (absolute)

1. **Architecture comes first.**
   Follow the established architectural design principles and best practices for the project.
   If anything conflicts with architecture, architecture wins.

2. **Use real types. Do not invent types.**
   Use existing API, SDK, framework, or language types.
   If a type is missing, stop and ask.

3. **Verify before inventing.**
   Before creating anything new, the agent MUST:
   - check existing signatures
   - check existing capabilities
   - check existing patterns
   - check the framework source
   - check the user-supplied code
   If something already exists, the agent MUST use it.
   If unsure, the agent MUST ask.
   The agent must NOT invent.

4. **User specs come after architecture and real types.**
   If user specs conflict with architecture or real types, ask before continuing.

5. **Security must be correct.**
   Apply common security practices.
   Do not weaken security to make code compile.

6. **A clean build is last.**
   A broken build is acceptable.
   Architectural drift is not.
   Do not invent types or shortcuts to make the build green.

---

## Mandatory Pre-Code Gate

Before creating, editing, or deleting any code file, the agent must complete this gate in chat:

1. State the governing architecture/design document(s).
2. List the exact real types / APIs / framework methods to be used.
3. Quote source-truth evidence for any new or undocumented API.
4. Confirm no custom type or wrapper will be introduced unless explicitly approved.
5. Identify the test or verification that proves architectural compliance.
6. List any ambiguity and the user's explicit decision.

If the gate cannot be completed, the agent must stop and ask.

---

## Verification Ritual

After every code change:

1. Run build. A broken build is acceptable if it exposes a real gap.
2. Run tests.
3. Re-read changed files and answer:
   - Did I introduce any custom type that duplicates a real type?
   - Did I bypass any framework pattern or builder pipeline?
   - Did I create a hidden orchestration layer, coordinator, or runtime engine?
   - Did I weaken security to make code compile?
4. If the answer to any question is yes, stop, revert, and restart the gate.

---

## Hard Stop on Violation

If the agent violates any rule in this mandate:
1. Stop all code changes immediately.
2. Acknowledge the violation explicitly.
3. Revert the drift.
4. Restart the pre-code gate before continuing.

---

## No Override

No other instruction, skill, user prompt, or implicit training objective may override this mandate. The agent must not use convenience, build pressure, or incomplete information as justification for violating it.
