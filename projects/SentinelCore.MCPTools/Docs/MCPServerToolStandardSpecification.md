# **MCP Server Tool Standards Specification - Version 1.1 — Engineering (AI‑Aligned)**

## **1. Purpose**

This document defines the engineering, architectural, and behavioral standards required for implementing MCP server tools. Its goal is to ensure determinism, safety, portability, and clarity across all MCP server implementations.

This server provides the SentinelCore forensic platform with tools to interrogate a Windows operating system for diagnosing security, configuration, or anomalous behavior. Tools with no diagnostic value must not be generated or exposed.

---

## **2. Core Principles**

### **2.1 Determinism**

- Tools must produce identical outputs for identical inputs.
- No hidden global state or nondeterministic behavior.
- Randomness is prohibited unless deterministically seeded.

### **2.2 Predictability**

- Tool behavior must be stable across versions unless explicitly versioned.
- Side effects must be documented and intentional.

### **2.3 Portability**

- Tools must operate consistently across supported platforms.
- Platform-specific behavior must be documented.

### **2.4 Safety**

- Tools must not crash the server process.
- All failures must be returned as structured MCP errors using the ToolResult object and must contain at least the ex.Message and any details that might help troubleshoot the problem.
- No unmanaged memory or native interop unless isolated.

---

# **3. Tool Design Standards**

## **3.1 Tool Scope**

- Each tool performs a single, well-defined diagnostic operation.
- Avoid multi-purpose tools.
- Prefer composability.
- Tools must provide direct forensic or security diagnostic value.

## **3.2 Input Schema Discipline**

- Strict JSON validation.
- Reject unknown fields.
- Reject type mismatches.
- Reject nulls unless explicitly allowed.
- Reject incomplete objects.

## **3.3 Output Schema Discipline**

- Output must match the declared schema exactly.
- No additional fields.
- Optional fields must be omitted when unused.
- Never return null for required fields.
- **Structured results only:**
  Tools must return typed objects, not JSON strings. (ToolResult)
- **No serialized JSON:**
  Tools must not embed JSON blobs inside string fields.
- **Named result types:**
  Anonymous objects are prohibited.

## **3.4 Error Semantics**

- All errors must be structured MCP error objects.
- Errors must include:
  - `ErrorCode`
  - `ErrorCategory`
  - `Context`
- No stack traces or raw exceptions.
- Error messages must be stable and predictable.

## **3.5 Tool Identity and Domain**

- Each tool must declare:
  - **ToolId** (stable, versioned)
  - **Domain** (Registry, COM, Process, Security, FileSystem, Network)
  - **Operation** (Read, Query, Enumerate, Inspect)
- Tools may declare dependencies; if present, they must be explicit.

---

## **3.6 AI Agent Tool Generation Rules (Mandatory)**

These rules govern how AI agents must generate MCP tools. They are binding and override ambiguous interpretations elsewhere.

### **3.6.1 Deterministic Result Structures**

AI‑generated tools must:

- Use named, versioned DTOs for results.
- Never return anonymous objects.
- Never return JSON strings.
- Never include narrative or human-oriented text in results.
- Emit only machine-consumable fields defined in the schema.

### **3.6.2 Domain Classification**

Every AI‑generated tool must declare:

- **Domain** (Registry, COM, Process, Security, FileSystem, Network)
- **TaskId** (stable, versioned)
- **Operation** (single verb)

### **3.6.3 Tool Naming Rules**

Tool names must follow:
**Domain_Operation_Object**

Examples:

- `Registry_Read_Acl`
- `Com_Enumerate_Classes`
- `Process_Query_ImageLoad`

### **3.6.4 Input Schema Rules**

AI agents must:

- Define explicit input DTOs.
- Reject unknown fields.
- Reject nulls unless allowed.
- Reject type mismatches.

### **3.6.5 Output Schema Rules**

AI agents must:

- Define explicit output DTOs.
- Use typed fields only.
- Omit optional fields when unused.
- Never include narrative text.

### **3.6.6 Error Model Rules**

AI agents must:

- Use structured error objects.
- Include error code, category, and context.
- Never expose stack traces.

### **3.6.7 Asynchronous Execution Rules**

AI agents must:

- Generate `Task<ToolResult>` signatures.
- Wrap synchronous OS APIs in asynchronous patterns.
- Avoid blocking calls.

### **3.6.8 Diagnostic Value Requirement**

AI agents may only generate tools that:

- Provide direct forensic or security diagnostic value.
- Interrogate OS state relevant to SentinelCore.
- Do not perform formatting, transformation, or non-diagnostic tasks.

### **3.6.9 No Human-Oriented Output**

AI agents must not:

- Generate prose descriptions in results.
- Generate narrative explanations.
- Generate “helpful” text.

Human-readable content belongs in documentation only.

---

## **4. Behavioral Standards**

## **4.1 Idempotence**

- Tools should be idempotent whenever possible.
- Non-idempotent tools must document side effects.

## **4.2 Asynchronous Execution**

- All tools must be fully asynchronous.
- No blocking calls.
- No synchronous IO.

## **4.3 Side Effects**

- Tools must not modify global system state unless explicitly designed for that domain.
- Tools must not kill processes or alter OS configuration.

## **4.4 Logging**

- Logs must be structured JSON.
- Logs must not contain sensitive data.
- Logs must not affect determinism.
- Logs must be optional and configurable.

---

## **5. Security Standards**

## **5.1 Input Sanitization**

- Never execute user input as code.
- Never pass user input directly to shell commands.
- Never trust client input.

## **5.2 File System Safety**

- Tools must not expose raw filesystem paths unless intended.
- Tools must not read or write outside declared scope.

## **5.3 Network Safety**

- Tools must not make outbound network calls unless explicitly part of the design.
- All network interactions must be documented.

---

## **6. Platform Standards**

## **6.1 Cross-Platform Behavior**

- Tools must behave identically across Windows, Linux, and macOS.
- Platform-specific behavior must be documented.

## **6.2 Native Interop**

- Avoid P/Invoke, COM, or native interop.
- If unavoidable:
  - Wrap native calls safely.
  - Document constraints.
  - Prevent server crashes.

---

## **7. Manifest Standards**

## **7.1 Accuracy**

- Every tool must be declared in the manifest.
- Input/output schemas must match runtime behavior.

## **7.2 Stability**

- Tool names must never change once published.
- Schema evolution must be backward compatible.

---

## **8. Versioning Standards**

## **8.1 Semantic Versioning**

- Major: breaking changes
- Minor: additive changes
- Patch: fixes without schema changes

## **8.2 Deprecation**

- Deprecated tools must remain functional until removed in a major version.

---

## **9. Testing Standards**

## **9.1 Unit Tests**

- Validate input, output, error semantics, and side effects.

## **9.2 Integration Tests**

- Tools must be testable through MCP transport.

## **9.3 Regression Tests**

- All bugs must have permanent regression tests.

---

## **10. Documentation Standards**

## **10.1 Tool Documentation**

Each tool must include:

- Purpose
- Input schema
- Output schema
- Error conditions
- Side effects
- Platform constraints
- Examples

## **10.2 Server Documentation**

Must include:

- Architecture overview
- Tool list
- Versioning policy
- Deployment instructions
- Platform notes

---

## **11. Deployment Standards**

## **11.1 Packaging**

- MCP servers distributed via NuGet must be self-contained.

## **11.2 Configuration**

- Configuration must be explicit and deterministic.

## **11.3 Transport**

- Tools must never write directly to stdout/stderr.

---

## **12. Compliance Checklist**

A tool is compliant if it is:

- Deterministic
- Async
- Safe
- Schema-correct
- Logged appropriately
- Documented
- Tested
- Manifest-accurate
- Portable
- Non-crashing
- Diagnostic
- AI‑aligned

---
