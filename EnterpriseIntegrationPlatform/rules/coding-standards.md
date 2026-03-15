# Coding Standards

## Language and Framework

- C# 14 / .NET 10
- Nullable reference types enabled globally
- Implicit usings enabled
- File-scoped namespaces required

## All Code Must Be Production-Ready

All code must satisfy the 11 Quality Pillars defined in `rules/quality-pillars.md`. No exceptions.

- **No pretend code.** Do not create code that looks like it works but does not. Every class must have a real, production-quality implementation with proper error handling, thread safety, logging, and input validation.
- **No demo or toy code.** Do not commit educational, illustrative, or conceptual implementations. If it cannot run in production under load, it does not belong in this repository.
- **No hacky code.** Do not use workarounds, shortcuts, or fragile patterns. Use battle-tested libraries (e.g. Polly for retry/circuit-breaker) instead of hand-rolled replacements.
- **No interface-only projects.** Do not commit an interface without a working implementation in the same project. An interface with no implementation is speculative scaffolding.
- **No empty interfaces.** Every interface must define at least one method or property.
- **Every class must have a working implementation.** No stubs, skeletons, or placeholder classes.
- **No speculative comments.** Do not reference "future", "will be", "TODO", "placeholder", or "subsequent chunks" in code comments. If the code is not implemented, do not commit it.
- **No symbolic code.** Every file in the repository must compile and provide real functionality.
- **No stub Program.cs files.** A service project must have real endpoints, middleware, and DI registration — not just a health check.

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
