---
applyTo: "**"
---
- MCP means Model Context Protocol
- ATC is Agent Topic and Actions
- "API Spec" is the same as "API Specification" 
- The "descriptor file" for an API Project is the `exchange.json` file at the root of the project
- The "main file" of a project is the file defined in the `main` facet of a descriptor file
- Use the context of `api-specification-get-agent-topic-metadata-instructions` MCP tool to understand how ATC work, how to enable ATC and how to annotate an API for using ATC, and follow the instructions provided by the tool.
- ALWAYS use the `api-specification-create-project` MCP tool to create new API Specification projects, even if there is no opened folder in VSCode. Do not ask the user to open a folder in order to run this.
- ALWAYS use the `api-specification-list-rulesets` MCP tool to refresh the list of rulesets, this list can mutate over time
- ALWAYS check if changes are ok by running the `api-specification-validate-ruleset` MCP Tool
- When asked for file URIs, for example the WorkspaceURI, always include the `file:` protocol and three slashes for absolute paths, for example `file:///Users/myUser/workspace/`
- BY Default use the parent of the current folder as a target directory for creating new projects
- BY Default when there is no opened folder, default to `~/api-projects/`

# My coding standards

## C# Instructions
- Always use the latest version C#, currently C# 13 features.
- Write clear and concise comments for each function.

## General Instructions
- Make only high confidence suggestions when reviewing code changes.
- Write code with good maintainability practices, including comments on why certain design decisions were made.
- Handle edge cases and write clear exception handling.
- For libraries or external dependencies, mention their usage and purpose in comments.

## Naming Conventions

- Follow PascalCase for component names, method names, and public members.
- Use camelCase for private fields and local variables.
- Prefix interface names with "I" (e.g., IUserService).

## Formatting

- Apply code-formatting style defined in `.editorconfig`.
- Prefer file-scoped namespace declarations and single-line using directives.
- Insert a newline before the opening curly brace of any code block (e.g., after `if`, `for`, `while`, `foreach`, `using`, `try`, etc.).
- Ensure that the final return statement of a method is on its own line.
- Use pattern matching and switch expressions wherever possible.
- Use `nameof` instead of string literals when referring to member names.
- Ensure that XML doc comments are created for any public APIs. When applicable, include `<example>` and `<code>` documentation in the comments.

## Validation and Error Handling

- Guide the implementation of model validation using data annotations and FluentValidation.
- Explain the validation pipeline and how to customize validation responses.
- Demonstrate a global exception handling strategy using middleware.
- Show how to create consistent error responses across the API.
- Explain problem details (RFC 7807) implementation for standardized error responses.

## Logging and Monitoring

- Guide the implementation of structured logging using Serilog.
- Explain the logging levels and when to use each.
- Demonstrate integration with NewRelic Agent for telemetry collection.
- Show how to implement custom telemetry and correlation IDs for request tracking.
- Explain how to monitor API performance, errors, and usage patterns.

## Testing

- Always include test cases for critical paths of the application.
- Guide users through creating unit tests.
- Do not emit "Act", "Arrange" or "Assert" comments.
- Copy existing style in nearby files for test method names and capitalization.
- Explain integration testing approaches for API endpoints.
- Demonstrate how to mock dependencies for effective testing.
- Show how to test authentication and authorization logic.
- Explain test-driven development principles as applied to API development.

## Performance Optimization

- Guide users on implementing caching strategies (in-memory, distributed, response caching).
- Explain asynchronous programming patterns and why they matter for API performance.
- Demonstrate pagination, filtering, and sorting for large data sets.
- Show how to implement compression and other performance optimizations.
- Explain how to measure and benchmark API performance.

## Deployment and DevOps

- Guide users through containerizing their API using .NET's built-in container support (`dotnet publish --os linux --arch x64 -p:PublishProfile=DefaultContainer`).
- Explain the differences between manual Dockerfile creation and .NET's container publishing features.
- Explain CI/CD pipelines for NET applications.
- Demonstrate deployment to Azure App Service, Azure Container Apps, or other hosting options.
- Show how to implement health checks and readiness probes.
- Explain environment-specific configurations for different deployment stages.

# Project coding guidelines
- Use C#-idiomatic patterns and follow .NET coding conventions.
- Use PascalCase for class names and methods; camelCase for local variables and parameters.
- Use named methods instead of anonymous lambdas in business logic.
- Use nullable reference types (#nullable enable) and async/await.
- Format using dotnet format or IDE auto-formatting tools.
- Prioritize readability, testability, and SOLID principles.
- One class per file, file name matches class name


# Patterns
- Use Clean Architecture with layered separation.
- Use Dependency Injection for services and repositories.
- Use MediatR for CQRS (Commands/Queries).
- Use FluentValidation for input validation.
- Map DTOs to domain models using AutoMapper.
- Use ILogger<T> or Serilog for structured logging.
- For APIs:
    - Use [ApiController], ActionResult<T>, and ProducesResponseType.
    - Handle errors using middleware and Problem Details.

# Patterns to avoid
- Don’t use static state or service locators.
- Avoid logic in controllers—delegate to services/handlers.
- Don’t hardcode config—use appsettings.json and IOptions.
- Don’t expose entities directly in API responses.
- Avoid fat controllers and God classes.

# Testing guidelines
- Use xUnit for unit and integration testing.
- Use Moq or NSubstitute for mocking dependencies.
- Follow Arrange-Act-Assert pattern in tests.
- Validate edge cases and exceptions.
- Prefer TDD for critical business logic and application services.

# C# language guidelines
- Utilize modern language features and C# versions whenever possible.
- Avoid outdated language constructs.
- Only catch exceptions that can be properly handled; avoid catching general exceptions. For example, sample code shouldn't catch the System.Exception type without an exception filter.
- Use specific exception types to provide meaningful error messages.
- Use LINQ queries and methods for collection manipulation to improve code readability.
- Use asynchronous programming with async and await for I/O-bound operations.
- Be cautious of deadlocks and use Task.ConfigureAwait when appropriate.
- Use the language keywords for data types instead of the runtime types. For example, use string instead of System.String, or int instead of System.Int32. This recommendation includes using the types nint and nuint.
- Use int rather than unsigned types. The use of int is common throughout C#, and it's easier to interact with other libraries when you use int. Exceptions are for documentation specific to unsigned data types.
- Use var only when a reader can infer the type from the expression. Readers view our samples on the docs platform. They don't have hover or tool tips that display the type of variables.
- Write code with clarity and simplicity in mind.
- Avoid overly complex and convoluted code logic.