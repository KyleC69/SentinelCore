---
description: "Create an Architectural Decision Record (ADR) for SentinelCore. Guides through the ADR template, assigns the next sequential number, fills frontmatter from the schema, and updates the ADR manifest."
name: "Create ADR"
argument-hint: "ADR title and brief description of the decision"
agent: "docs-steward"
tools: [read, edit, search, create_file, list_dir, file_search, grep_search]
---

Create a new Architectural Decision Record (ADR) for the SentinelCore project.

## Inputs

The user provides:
- **Title**: A concise decision title (from the prompt argument or chat)
- **Decision**: What was decided
- **Rationale**: Why this decision was made
- **Consequences**: What impact this has on the codebase

If any of these are missing, ask the user to provide them before proceeding.

## Steps

1. **Read the ADR manifest** at `/docs/decisions/ADR-Manifest.md` to find the highest existing ADR number. The next ADR number is that + 1, zero-padded to 4 digits (e.g. `0001`, `0002`).

2. **Read the pattern-lock** at `/architecture/pattern-lock.md` and identify which sections or rules the ADR affects. List them as `violations` in the frontmatter if the decision breaks a locked pattern. If the decision reinforces or adds a pattern, note it in the rationale.

3. **Identify affected paths** — search the codebase for files impacted by the decision and list them in the `affected_paths` frontmatter field.

4. **Create the ADR file** at `/docs/decisions/ADR-XXXX.md` using the template format:

```markdown
---
id: ADR-XXXX
title: "Decision Title"
status: Proposed
author: Kyle
date: YYYY-MM-DD
affected_paths:
  - path/to/file.cs
breaking: false
violations: []
---

# Decision

{What was decided}

# Rationale

{Why this decision was made — reference pattern-lock sections if relevant}

# Consequences

{Impact on codebase, tests, future work — note any pattern-lock updates needed}
```

5. **Update the ADR manifest** at `/docs/decisions/ADR-Manifest.md` — append a new row to the table:

```markdown
| ADR-XXXX | Decision Title | Proposed | YYYY-MM-DD | No |
```

6. **Report back** to the user:
   - The ADR file path created
   - The ADR number and title
   - Whether it breaks any pattern-lock rules
   - Whether the pattern-lock needs updating as a follow-up
