
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
- **Config**: Read from environment variables with `UPPER_SNAKE_CASE` naming
- **Tests**: Add Arrange/Act/Assert comments; use Moq for mocking; test methods returning `Task`/`ValueTask` must use the `Async` suffix.
- **Testing**: Tests should be written to be deterministic and not rely on external state. Use mocking frameworks to isolate dependencies and ensure consistent test results. They should be created to catch drift and state changes. Mocking should be used as little as possible and targeting 3rd party API only whenever possible.

## Key Design Principles

When developing or reviewing code, verify adherence to these key design principles:

- **DRY**: Avoid code duplication by moving common logic into helper methods or helper classes.
- **Single Responsibility**: Each class should have one clear responsibility.
- **Encapsulation**: Keep implementation details private and expose only necessary public APIs.
- **Strong Typing**: Use strong typing to ensure that code is self-documenting and to catch errors at compile time.

## Version Pinning

This release is intended to be compatible with Windows 10 and .NET 10 Do Not install .NET 11 packages.

## Structural Mandates

Some components in this repository are required to be implemented as optional and in a specific way. These components must adhere to the following structural mandates:

- It is mandatory for their seams to be clearly defined. Their implementation must be isolated and cause zero friction for the rest of the system. 
- They must not introduce any breaking changes to the system.
- They must not introduce any new dependencies to the system.
- They must be clearly documented as optional components.
- While working in this repository, you may encounter optional components that are *NOT* implemented according to this mandate, you should take steps to correct any issues you find. If you are unsure, alert the user and explain your findings so a decision can be made. 

The initial optional components are:

Case Flow Engine: Maintains tracking of investigations and persists to a database. It is optional and system can run with or without it.
RAG Engine: Maintains a vector database and provides retrieval augmented generation capabilities. It is optional and system can run with or without it.