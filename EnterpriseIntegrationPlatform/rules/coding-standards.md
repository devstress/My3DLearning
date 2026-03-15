# Coding Standards

## Language and Framework

- C# 14 / .NET 10
- Nullable reference types enabled globally
- Implicit usings enabled
- File-scoped namespaces required

## No Conceptual or Abstract Code

- **Every interface must define at least one method or property.** No empty interfaces.
- **Every class must have a working implementation.** No stubs, skeletons, or placeholder classes.
- **No speculative comments.** Do not reference "future", "will be", "TODO", "placeholder", or "subsequent chunks" in code comments. If the code is not implemented, do not commit it.
- **No symbolic code.** Every file in the repository must compile and provide real functionality.

## Naming Conventions

- PascalCase for public members, types, namespaces
- camelCase for private fields (prefixed with underscore: `_fieldName`)
- UPPER_CASE for constants
- Interfaces prefixed with `I`
- Async methods suffixed with `Async`

## Project Structure

- One class per file (except small related DTOs)
- Folder structure mirrors namespace hierarchy
- `Internal` folder for implementation details
- `Abstractions` folder for interfaces

## Code Style

- Use expression-bodied members where concise
- Prefer pattern matching over type checking
- Use records for immutable data types
- Use `required` keyword for mandatory properties
- Use primary constructors where appropriate

## Error Handling

- Never swallow exceptions silently
- Use Result pattern for expected failures
- Use exceptions for unexpected failures
- Always log exceptions with structured data
- Use `ActivitySource` for distributed tracing context

## Dependency Injection

- Register services via extension methods (`AddXxx`)
- Use `IOptions<T>` for configuration binding
- Prefer constructor injection
- Avoid service locator pattern

## Testing

- xUnit for unit tests
- Arrange-Act-Assert pattern
- One assertion per test (logical)
- Use `FluentAssertions` for readable assertions
- Use `NSubstitute` for mocking
- Test naming: `MethodName_Scenario_ExpectedResult`

## Documentation

- XML docs on all public APIs
- Architecture Decision Records (ADRs) for significant decisions
- Update `milestones.md` after each chunk

## Version Control

- Conventional commits: `feat:`, `fix:`, `docs:`, `refactor:`, `test:`, `chore:`
- One logical change per commit
- PR description must reference chunk ID
