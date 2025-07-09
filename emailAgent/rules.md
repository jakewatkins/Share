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