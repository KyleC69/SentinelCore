# Copilot Instructions

## Related Constraint Documentation

[../instructions/drift-prevention.instructions.md](../instructions/drift-prevention.instructions.md) - Overview of the architecture of Sentinel Core and its components.

[../instructions/ProjectTerminology.instructions.md](../instructions/ProjectTerminology.instructions.md) - Overview of the terminology used in Sentinel Core and its components.

[../../architecture/pattern-lock.md](../../architecture/pattern-lock.md) - Single source of truth for the patterns used in Sentinel Core and its components. Used to prevent architectural drift.


Instructions for AI coding agents working in the Sentinel Core Platform Repository

## Sentinel Core is rooted in MAF and the types contained in

### Core types

- `AIAgent`: The abstract base class that all agents derive from, providing common methods for interacting with an agent.
- `AgentSession`: The abstract base class that all agent sessions derive from, representing a conversation with an agent.
- `ChatClientAgent`: An `AIAgent` implementation that uses an `IChatClient` to send messages to an AI provider and receive responses.
- `IChatClient`: Interface for sending messages to an AI provider and receiving responses. Used by `ChatClientAgent` and implemented by provider-specific packages.
- `FunctionInvokingChatClient`: Decorator for `IChatClient` that adds function invocation capabilities.
- `AITool`: Represents a tool that an agent/AI provider can use, with metadata and an execution delegate.
- `AIFunction`: A specific type of `AITool` that represents a local function the agent/AI provider can call, with parameters and return types defined.
- `ChatMessage`: Represents a message in a conversation.
- `AIContent`: Represents content in a message, which can be text, a function call, tool output and more.

- If conflict is detected or type missing, check MAF API's source before creating ad-hoc version.
- MAF is still under developement expect changes.

## Key Conventions

- **Command output capture**: When running large commands(eg. commands producing large results) or expensive commands, redirect output to a temp file first (e.g., `dotnet build --tl:off 2>&1 | Out-File $env:TEMP\build.log`), then analyze the file as needed. This avoids re-running expensive commands when the initial analysis misses something.
- **XML docs**: Required for all methods and classes. Do not use    /// <inheritdoc />
- **Async**: Use `Async` suffix for methods returning `Task`/`ValueTask`
- **Private classes**: Should be `sealed` unless subclassed
- **Nullability**: Ensure all new code is nullability annotated and warnings are enabled. Use `#nullable enable` at the top of each file.
- **Config**: Read from environment variables with `UPPER_SNAKE_CASE` naming
- **Tests**: Add Arrange/Act/Assert comments; use Moq only when absolutely necessary for mocking; test methods returning `Task`/`ValueTask` must use the `Async` suffix.
- **Testing**: Tests should be written to be deterministic and also check for abnormal state changes. Adversarial tests should also be created. Use mocking frameworks to isolate dependencies and ensure consistent test results. They should be created to catch drift and state changes. Mocking should be used as little as possible and targeting 3rd party API only whenever possible.
- **Attributes**: Use attributes to provide metadata and configuration for classes and methods. Avoid using attributes for logic or behavior.
- **Exception handling**: Use try/catch blocks to handle exceptions and provide meaningful error messages. Use the "Throw" helper methods for argument validation and error propagation.


## Key Design Principles

When developing or reviewing code, do not introduce code that may violate established professional grade design principles. The following are a few of the principles that should be followed:

- **DRY**: Avoid code duplication by moving common logic into helper methods or helper classes.
- **Single Responsibility**: Each class should have one clear responsibility.
- **Encapsulation**: Keep implementation details private and expose only necessary public APIs.
- **Strong Typing**: Use strong typing to ensure that code is self-documenting and to catch errors at compile time.
- **Inheritance-based architecture**: Use inheritance-based architecture to encode workflow invariants.
Shared behavior, required steps, and execution guarantees must be implemented in abstract base classes, not duplicated across derived components.
Derived classes should only supply variable behavior, never redefine the invariant structure.

## Version Pinning

This release is intended to be compatible with Windows 10 and .NET 10 Do Not install .NET 11 packages.

## Structural Mandates

Some components in this repository are intended to be optional or add-ons to the original program and must be implemented in a specific way. These components must adhere to the following structural mandates:

- It is mandatory for their seams to be clearly defined. Their implementation must be isolated and cause zero friction for the rest of the system. 
- They must not introduce any breaking changes to the system when added or when taken out. The system must be able to function without them.
- They must not introduce any new dependencies to the system.
- They must be clearly documented as optional components.
- While working in this repository, you may encounter optional components that are *NOT* implemented according to this mandate, you should take steps to correct any issues you find. If you are unsure, alert the user and explain your findings so a decision can be made. 

## Optional Components (Deluxe Edition)

	Satelite anomaly detectors: Specialty anomaly detectors that can be installed to servers running particular workloads. They are optional and system can run with or without them.
		For example firewalls, SQL Servers, propriatary software, etc. These detectors are not part of the core system and are not required for the system to function. They are designed to provide additional functionality and insights for specific workloads.
		Signals are sent back to the core system for analysis and correlation with other signals. They are designed to be modular and can be added or removed as needed without affecting the core system's functionality.

	RAG Engine: Maintains a vector database and provides retrieval augmented generation capabilities. It is optional and system can run with or without it.

	Workflow expansion pack: Provides additional workflow capabilities and integrations. It allows for more complex and customizable workflows to be created and managed within the system. It is optional and system can run with or without it.

## Unit Testing 

Generate behavior-focused tests.

Rules:
- Test real code whenever possible.
- **Mock only process boundaries and external systems.**
- Avoid verifying implementation details.
- Prefer state verification over interaction verification.
- Tests must be readable in under 30 seconds.
- The test setup should be smaller than the code under test.
- Focus on observable outcomes.
- Prioritize realistic failure cases.