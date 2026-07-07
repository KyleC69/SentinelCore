---
name: Architecture Gate
skill: Architecture Gate
description: Forces the agent to complete the Universal Architectural Priority Mandate before writing or modifying any code. Invoke before every coding task.
---

# Architecture Gate

Complete this gate in chat before touching any code file.

## Priority Reminder

1. Architecture comes first.
2. Use real types. Do not invent types.
3. Verify before inventing: check signatures, capabilities, patterns, framework source, user code.
4. User specs come after architecture and real types.
5. Security must be correct.
6. A clean build is last — broken builds are acceptable; drift is not.

## Gate Checklist

- [ ] **Governing doc(s):** Which design/architecture document(s) apply?
- [ ] **Component role:** CFE / Core / Manager / Agent Framework / Tools / Memory / Safety Rails
- [ ] **Real types to use:** Exact framework / SDK / API types and methods
- [ ] **Source-truth evidence:** Quote from framework source or docs for any new/undocumented API
- [ ] **No custom types:** Confirm no wrapper/provider/abstraction will be introduced unless explicitly approved
- [ ] **Security check:** Does this change preserve or strengthen security?
- [ ] **Verification test:** What test/check proves compliance?
- [ ] **Ambiguities / user decisions:** List anything unclear and the user's direction

## Stop Conditions

Stop and ask the user if:
- A needed type appears missing.
- The framework source contradicts the intended approach.
- The design doc is ambiguous.
- User instructions conflict with architecture or real types.

Do NOT proceed until the gate is complete and approved.
