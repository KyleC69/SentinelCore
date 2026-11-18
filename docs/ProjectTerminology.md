---
title: "Project Terminology"
status: Active
component: Glossary
last_updated: 2026-07-10
---

# Project Terminology

This glossary defines current project terms and distinguishes implemented roles from proposed dynamic-agent concepts.

## Canonical Terms

- **SentinelCore:** A Windows-oriented, multi-agent investigation and case-management platform built on Microsoft Agent Framework and .NET 10.
- **AIAgent / Agent:** A model-backed MAF agent created through `ISentinelAgentFactory` from an `AgentPreset`.
- **AgentPreset:** The enum value that selects a role definition. `AgentPresetDefinition` supplies identity, instructions, model tier, and feature flags.
- **ModelTier:** The configuration category used by `IModelResolver` to select a `ModelProfile` from `SentinelCoreSettings.TierModels`.
- **AITool:** An invocable deterministic tool exposed to an agent. Tools may come from MCP servers or in-process `AIFunction` registrations.
- **The Core:** The persistent conversational agent and primary reasoning surface used for direct responses and case-oriented interpretation.
- **Manager Agent:** The tool-free Magentic workflow coordinator. It coordinates investigation participants and is subject to pattern lock PL-3.
- **Classifier Agent:** A short-lived, tool-free agent used by the workflow to classify an incoming signal before investigation dispatch.
- **Safety Agent and Safety Executor:** Safety-related surfaces are distinct. The active workflow gate is `SafetyExecutor`; `SafetyEngineAgent` is a separate rule-engine adapter and is not the same execution path.
- **Worker Agent:** A preset used to create named Magentic participants such as evidence collector, adversarial analyst, and researcher, with role-derived or explicit tools.
- **Aggregator Agent:** A preset used for the final report writer that receives the investigation-team output.
- **Core Worker:** A proposed reusable core-assistant concept; it should not be confused with the `WorkerAgent` presets created in the active Magentic sub-workflow.
- **Magentic Workflow:** MAF's manager-and-participants workflow used as the evidence-gathering sub-workflow inside `TheCoreWorkflow`.
- **Domain Agent / Dynamic Agent:** Historical or proposed concepts. Current tool specialization is selected through `AgentToolSelection` on preset-based agents rather than through the removed `DomainAgentFactory` design.
- **Pattern Memory:** Persistence and matching concepts for prior case patterns. The storage and matcher surfaces exist, but automatic injection is disabled pending stabilization.
- **Optional Persistence:** `AddSentinelCore` installs null case-flow defaults; the host opts into EF persistence with `AddCaseFlowEngine` and a registered `IDbContextFactory`.

Use this document when resolving unfamiliar project terms or deciding which implementation concept a legacy document is describing.

---

## Change Table

| Version | Date | Author | Change summary |
| --- | --- | --- | --- |
| 2.0.0 | unknown | unknown | Replaced obsolete builder, role, provider, and dynamic-agent terminology with current preset, tier, workflow, and optional-persistence definitions. |
