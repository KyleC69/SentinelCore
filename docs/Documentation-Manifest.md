---
title: "Documentation Manifest"
status: Active
component: Index
last_updated: 2026-11-20
---

# Documentation Manifest

This manifest inventories current implementation references, historical design notes, proposals, checklists, and architecture decisions in SentinelCore.

## Document Index

| Document | Classification | Authority and current use |
| --- | --- | --- |
| [GettingStarted.md](./GettingStarted.md) | Current reference | Host integration: settings, DI registration, events, first investigation, custom safety rules, and MCP server registration. |
| [SolutionArchitecture.md](./SolutionArchitecture.md) | Current reference | Solution membership, project dependencies, host composition, and registration flow. |
| [ContractsComponent.md](./ContractsComponent.md) | Current status plus historical reference | Shared contract boundary; older model examples below the status banner are non-authoritative. |
| [OrchestrationComponent.md](./OrchestrationComponent.md) | Current status plus historical reference | Current orchestration entry points and workflow; former seven-strategy design retained below the banner. |
| [DynamicAgentsComponent.md](./DynamicAgentsComponent.md) | Current status plus historical reference | Current preset/factory agent construction; builder/spec pipeline retained as legacy. |
| [ToolingComponent.md](./ToolingComponent.md) | Current reference | Prefix-based tool groups, preset-owned tool assignments, selection kinds, and uncategorized-tool handling. |
| [CaseFlowEngineComponent.md](./CaseFlowEngineComponent.md) | Current status plus historical reference | Optional module registration and lifecycle boundary; older API examples are non-authoritative. |
| [CaseManagement.md](./CaseManagement.md) | Proposal | Unimplemented case-management data model. |
| [ExampleCaseSchema.md](./ExampleCaseSchema.md) | Proposal/example | Illustrative data only, not the active EF schema. |
| [PersistenceComponent.md](./PersistenceComponent.md) | Current status plus historical model reference | Factory-based `DbContext` rules and host opt-in; generated source remains schema authority. |
| [MemoryLayerComponent.md](./MemoryLayerComponent.md) | Partial implementation/target design | Contracts and matcher exist; context injection is disabled and storage/search are incomplete. |
| [McpServerRegistryComponent.md](./McpServerRegistryComponent.md) | Current reference | MCP registry, persistence, startup, UI, and tool publication. |
| [SafetyEngineComponent.md](./SafetyEngineComponent.md) | Current reference with caveats | Active safety executor, rules, and known metadata-routing gaps. |
| [SecurityComponent.md](./SecurityComponent.md) | Current reference | Message-provenance signing, identity registry stub, multi-point fail-closed validation, quarantine, and the SentinelCoreService pathway plan. |
| [SafetyRailsComponent.md](./SafetyRailsComponent.md) | Superseded | Historical middleware-oriented safety design. |
| [SafetyEngine-Implementation-Checklist.md](./SafetyEngine-Implementation-Checklist.md) | Historical checklist | Migration/status history; not authoritative for runtime claims. |
| [Safety-Valve-Overview.md](./Safety-Valve-Overview.md) | Proposal/overview | Intended safety behavior. |
| [Proposed-SafetyEngine-Group-Collaboration.md](./Proposed-SafetyEngine-Group-Collaboration.md) | Proposal | Target collaboration model for safety handling. |
| [SafetyEngine-Transcript-Design-Plan.md](./SafetyEngine-Transcript-Design-Plan.md) | Proposal/design input | Transcript-derived target design. |
| [workflowDiagram.md](./workflowDiagram.md) | Current reference | Active `TheCoreWorkflow` graph and unwired branches. |
| [ProjectTerminology.md](./ProjectTerminology.md) | Current glossary with caveats | Canonical terms; runtime roles and implementation must still be checked against source. |
| [Documentation-Manifest.md](./Documentation-Manifest.md) | Current index | This document. |
| [../architecture/pattern-lock.md](../architecture/pattern-lock.md) | Authoritative architecture rules | Locked patterns enforced by review and architecture tests. |

## Architecture Decision Records

| Document | Classification | Current disposition |
| --- | --- | --- |
| [ADR/001-DTO-EF-Boundary.md](./ADR/001-DTO-EF-Boundary.md) | ADR review needed | Current implementation exposes EF entities through parts of the persistence-facing boundary, so the ADR should be amended or superseded. |
| [ADR/002-MCP-Timeout-And-Async-IO-Strategy.md](./ADR/002-MCP-Timeout-And-Async-IO-Strategy.md) | Proposed, largely represented in source | Cancellation and timeout behavior exists in current paths, but the ADR itself remains Proposed and should be formally accepted or revised. |
| [ADR/003-Agent-Tool-Selection.md](./ADR/003-Agent-Tool-Selection.md) | Accepted with minor drift | Implemented selection model; examples must use current registered `local_case_*` names. |
| [ADR/004-Package-Version-Pinning.md](./ADR/004-Package-Version-Pinning.md) | Accepted | Decision remains current; build observations are historical snapshots. |
| [ADR/005-Tool-Domain-Vocabulary.md](./ADR/005-Tool-Domain-Vocabulary.md) | Superseded by ADR-006 | Historical only; the rule-based domain vocabulary is no longer implemented. |
| [ADR/006-Tool-Group-Prefix-Scheme.md](./ADR/006-Tool-Group-Prefix-Scheme.md) | Accepted/implemented | Prefix-based tool groups; preset-owned assignments with an optional call-site override. UI surfacing of empty-group and uncategorized warnings remains incomplete. |
| [ADR/008-Message-Provenance-Security-Module.md](./ADR/008-Message-Provenance-Security-Module.md) | Accepted | HMAC-SHA256 message provenance stamped via `AdditionalProperties`, fail-closed multi-point validation, as-is quarantine, and a registry-validated identity model. |

## Component READMEs

| Document | Classification | Current use |
| --- | --- | --- |
| [../README.md](../README.md) | Current overview | Public and investor-facing entry point; integration detail lives in [GettingStarted.md](./GettingStarted.md). |
| [../projects/SentinelCore.Orchestrations/Mcp/README.md](../projects/SentinelCore.Orchestrations/Mcp/README.md) | Current reference with caveats | MCP integration architecture and usage. |
| [../projects/SentinelCore.Orchestrations/Agents/Core/Tools/README.md](../projects/SentinelCore.Orchestrations/Agents/Core/Tools/README.md) | Current component note | In-process case and documentation tools. |

## Omitted Core Sections

| Section | Reason omitted |
| --- | --- |
| Setup and installation guide | Prerequisites and registration steps are covered in the repository README; no separate dedicated guide exists yet. |
| Configuration reference | Configuration is distributed across the README, model configuration UI, and component documents; no consolidated reference exists yet. |
| Operational runbook | No production service operations or incident-response procedure has been documented. |
| Deployment and release process | No stable release/deployment procedure is defined in the repository. |
| Testing strategy | Test projects and architecture tests exist, but a dedicated testing-strategy document has not been authored. |
| Project-specific security notes | Message provenance and quarantine are documented in [SecurityComponent.md](./SecurityComponent.md); a consolidated threat and data-handling guide covering safety, tooling, and provenance together does not exist yet. |

Use this manifest when locating an authoritative reference or determining whether a document is current, historical, or proposed.

## Change Table

| Version | Date | Author | Change summary |
| --- | --- | --- | --- |
| 3.0.0 | unknown | unknown | Rebuilt the manifest to inventory all direct docs, ADRs, and READMEs; added classification, authority, and omissions. |
| 3.1.0 | 2026-11-20 | Copilot | Added SecurityComponent.md and ADR-008; registered the SentinelCoreSecurity message-provenance module in the document index, ADR table, and security-notes entry. |
