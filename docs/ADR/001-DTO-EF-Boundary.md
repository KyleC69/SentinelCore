## Architecture Decision Record: DTO-to-EFCore Boundary Definition (ADR-001)

**Status:** Proposed
**Date:** 2026-10-13
**Architects:** [Agent/Copilot]
**Impacted Components:** `Contracts/` (DTOs), `CaseFlowEngine/`, `SentinelCore.Tests/`

---

### 1. Context and Problem Statement

The `CaseFlowEngine` component, which represents the core domain logic, currently relies on data transfer objects (DTOs) residing in the `Contracts/` project for its input/output signatures. This creates a strong coupling between the core business logic and the presentation/API layer's data shape. This is a violation of the Port and Adapters pattern, as the core domain should only depend on its own stable, internal interfaces.

The persistence layer relies on explicit EF Core entities (e.g., `CaseEntity`, `SignalEntity`) which are the true source of truth for data persistence.

### 2. Proposed Solution

We propose establishing a strict, explicit **Data Transfer Object (DTO) $\rightarrow$ EF Entity Mapping Boundary**.

1. **Source of Truth:** The EF Core Entity model (the `Entity` classes) remains the single source of truth for persistence.
2. **Mapping Contract:** Every DTO class in the `Contracts/` project that needs to be persisted must implement a mapping method, `public TEntity ToEntity()`, where `TEntity` is the corresponding EF Entity type.
3. **Usage Scope:** The `CaseFlowEngine` must be modified to:
    a. **Input:** Accept DTOs at its public API boundaries.
    b. **Conversion:** Immediately convert the input DTO to the required EF Entity type using the DTO's `ToEntity()` method *at the service layer boundary*.
    c. **Internal Logic:** Operate solely on the EF Entity type internally.

### 3. Architectural Principles Applied

* **Separation of Concerns:** The API boundary (DTOs) is separate from the persistence implementation (EF Entities).
* **Explicit Contracts:** All data transformations are explicit via the `ToEntity()` method, making the mapping transparent and testable.
* **Dependency Rule:** Dependencies flow inward: `CaseFlowEngine` depends only on `Contracts` (for the DTO input signature) and `Contracts` (for the mapping interface), but it operates entirely on the `Entity` model, isolating it from the DTO structure.

### 4. Implementation Steps (See ADR-001 Refactoring Plan)

(This section is a reference to the detailed plan provided in the chat history, listing the phases: DTO Update $\rightarrow$ EF Hardening $\rightarrow$ CFE Refactoring.)

### 5. Verification Strategy

1. **Unit Testing:** Mandatory unit tests for every `ToEntity()` method.
2. **Build Test:** A full build to confirm zero compile errors related to data access.
3. **System Integration Test:** End-to-end tests must verify that data correctly flows from a mocked DTO input through the service layer, resulting in a persistence model that matches the expected database state.

---
**Conclusion:** This approach formalizes the mapping process, drastically reducing the surface area for architectural drift related to data structure changes.
"
