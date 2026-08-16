# AGENTS.md

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
- **Encoding**: All new files must be saved with UTF-8 encoding with BOM (Byte Order Mark). This is required for `dotnet format` to work correctly. When using PowerShell `Set-Content`, always pass `-Encoding UTF8BOM` to preserve the BOM (e.g., `Set-Content $file $content -NoNewline -Encoding UTF8BOM`).
- **Copyright header**: 'Supplied by Resharper, do not touch existing blocks'
- **XML docs**: Required for all methods and classes. Do not use //inherit>
- **Async**: Use `Async` suffix for methods returning `Task`/`ValueTask`
- **Private classes**: Should be `sealed` unless subclassed
- **Config**: Read from environment variables with `UPPER_SNAKE_CASE` naming
- **Tests**: Add Arrange/Act/Assert comments; use Moq for mocking; test methods returning `Task`/`ValueTask` must use the `Async` suffix.

## Key Design Principles

When developing or reviewing code, verify adherence to these key design principles:

- **DRY**: Avoid code duplication by moving common logic into helper methods or helper classes.
- **Single Responsibility**: Each class should have one clear responsibility.
- **Encapsulation**: Keep implementation details private and expose only necessary public APIs.
- **Strong Typing**: Use strong typing to ensure that code is self-documenting and to catch errors at compile time.
