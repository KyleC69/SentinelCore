
## Key Conventions

- **Command output capture**: When running `dotnet build`, `dotnet test`, `dotnet format`, or similar commands, redirect output to a temp file first (e.g., `dotnet build --tl:off 2>&1 | Out-File $env:TEMP\build.log`), then analyze the file as needed. This avoids re-running expensive commands when the initial analysis misses something.
- **Encoding**: All new files must be saved with UTF-8 encoding with BOM (Byte Order Mark). This is required for `dotnet format` to work correctly. When using PowerShell `Set-Content`, always pass `-Encoding UTF8BOM` to preserve the BOM (e.g., `Set-Content $file $content -NoNewline -Encoding UTF8BOM`).

- **XML docs**: Required for all methods and classes
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
