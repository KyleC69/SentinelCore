---
title: "Project Terminology"
status: Active
component: Glossary
last_updated: 2026-07-10
---

### SentinelCore technical flow (integrated with canonical terminology)

---

### 1. Canonical terminology (short form)

- **OllamaApiClient:** Chat client or base client provider used for all AIAgents (from OllamaSharp 5+).
- **AIAgent / Agent:** Model‑backed AI agent (Agent Framework 1.10+), using `OllamaApiClient`. Can reason, call AITools, and follow instructions. No hardcoded logic.
- **AITool:** Deterministic function (Agent Framework 1.14+). Non‑AI, C# function performs system operations (read registry, query WMI, etc.). Only callable by an AIAgent.
- **The Core or core agent:** A dedicated `AIAgent` responsible for generating investigation plans, interpreting results, updating hypotheses, and driving the investigative loop. The Core is the only reasoning entity in SentinelCore. It runs its own LLM model and configuration settings, distinct from the Manager.
- **Manager (Magnetic Orchestration Managing Agent):** A separate, dedicated `AIAgent` that receives the investigation plan from the Core and executes it as a magnetic orchestration workflow. It schedules tasks, dispatches Domain Agents as workflow participants, monitors progress, enforces ordering, and returns structured results back to the Core. It does not reason, modify the plan, or generate new tasks. The Manager uses its own LLM model and configuration settings, distinct from the Core.
- **Domain Agent:** A predefined, reusable `AIAgent` definition. The Manager hands it a bounded task and a skill (tools + instructions) for a single workflow step. Domain Agents are participants in the Manager's orchestration workflow.
- **Composite / Dynamic Agent:** A special `AIAgent` created by the Manager when a task in the Core's plan requires combined cross-domain skills. These agents may perform _local micro‑reasoning_ (comparison, summarization, cross‑domain correlation), but they do not perform global investigative reasoning. They are always short‑lived and plan‑bound.
- **Session model:**
  - **Core Agent:** Maintains its own isolated session state, enriched by middleware, RAG, pattern memory, and case context.

  - **Manager:** Maintains a workflow session shared with its subagents. No RAG, no enrichment, no reasoning.

  - **Domain / Composite Agents:** Stateless. No persistent session. Receive only the skill, task, and toolbelt for the current invocation.
- **Pattern Memory:** Pattern Memory stores vectorized signals, case metadata, anomaly signatures, and resolution summaries. It is queried only by the Core Agent. It is updated only when a case closes.

---
