using EmailServices.Api.Services;
using EmailServices.Api.DTOs;
using Serilog;
using Serilog.Formatting.Json;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog for structured logging
builder.Host.UseSerilog((context, config) =>
    config.WriteTo.Console(new JsonFormatter())
          .WriteTo.File("/logs/emailapi.log", rollingInterval: RollingInterval.Day)
          .MinimumLevel.Information());

// Add services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Email Services API", Version = "v1" });
});
builder.Services.AddHealthChecks();

// Register application services
builder.Services.AddScoped<IEmailOperationsService, EmailOperationsService>();
builder.Services.AddSingleton<ILoggerFactory, LoggerFactory>();

var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Health check endpoints
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false
});

// API endpoints
app.MapGet("/api/v1/emails", async (
    string service,
    string userEmail,
    int count,
    IEmailOperationsService emailService,
    ILogger<Program> logger) =>
{
    try
    {
        var correlationId = Guid.NewGuid().ToString();
        logger.LogInformation("Processing get emails request for {Service}/{UserEmail} with correlation {CorrelationId}",
            service, userEmail, correlationId);

        var result = await emailService.GetEmailsAsync(service, userEmail, count > 0 ? count : 10);

        return Results.Ok(new ApiResponse<object>(true, result));
    }
    catch (ArgumentException ex)
    {
        return Results.Problem(
            statusCode: 400,
            title: "Invalid request",
            detail: ex.Message);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error processing get emails request for {Service}/{UserEmail}", service, userEmail);
        return Results.Problem(
            statusCode: 500,
            title: "Internal server error",
            detail: "An error occurred while retrieving emails");
    }
})
.WithName("GetEmails");

app.MapDelete("/api/v1/emails/{emailId}", async (
    string emailId,
    string service,
    string userEmail,
    IEmailOperationsService emailService,
    ILogger<Program> logger) =>
{
    try
    {
        var correlationId = Guid.NewGuid().ToString();
        logger.LogInformation("Processing delete email request for {EmailId} with correlation {CorrelationId}",
            emailId, correlationId);

        var result = await emailService.DeleteEmailAsync(emailId, service, userEmail);

        if (result)
        {
            return Results.Ok(new ApiResponse<object>(true, new { EmailId = emailId, Deleted = true }));
        }

        return Results.Problem(
            statusCode: 500,
            title: "Email deletion failed",
            detail: $"Failed to delete email {emailId}");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error processing delete request for email {EmailId}", emailId);
        return Results.Problem(
            statusCode: 500,
            title: "Internal server error",
            detail: "An error occurred while processing the email deletion request");
    }
})
.WithName("DeleteEmail");

app.MapPut("/api/v1/emails/{emailId}/move", async (
    string emailId,
    string service,
    string userEmail,
    string folder,
    IEmailOperationsService emailService,
    ILogger<Program> logger) =>
{
    try
    {
        var correlationId = Guid.NewGuid().ToString();
        logger.LogInformation("Processing move email request for {EmailId} to folder {Folder} with correlation {CorrelationId}",
            emailId, folder, correlationId);

        var result = await emailService.MoveEmailAsync(emailId, service, userEmail, folder);

        if (result)
        {
            return Results.Ok(new ApiResponse<object>(true, new { EmailId = emailId, Moved = true, Folder = folder }));
        }

        return Results.Problem(
            statusCode: 500,
            title: "Email move failed",
            detail: $"Failed to move email {emailId} to folder {folder}");
    }
    catch (NotImplementedException)
    {
        return Results.Problem(
            statusCode: 501,
            title: "Move operation not implemented",
            detail: $"Move operation for {service} is not yet implemented");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error processing move request for email {EmailId}", emailId);
        return Results.Problem(
            statusCode: 500,
            title: "Internal server error",
            detail: "An error occurred while processing the email move request");
    }
})
.WithName("MoveEmail");

app.MapPost("/api/v1/folders", async (
    string service,
    string userEmail,
    string folderName,
    IEmailOperationsService emailService,
    ILogger<Program> logger) =>
{
    try
    {
        var correlationId = Guid.NewGuid().ToString();
        logger.LogInformation("Processing create folder request for {Service}/{UserEmail} folder {FolderName} with correlation {CorrelationId}",
            service, userEmail, folderName, correlationId);

        var result = await emailService.CreateFolderAsync(service, userEmail, folderName);

        if (result.Success)
            return Results.Ok(new ApiResponse<object>(true, result));

        return Results.Problem(
            statusCode: 500,
            title: "Folder creation failed",
            detail: result.Message);
    }
    catch (ArgumentException ex)
    {
        return Results.Problem(statusCode: 400, title: "Invalid request", detail: ex.Message);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error creating folder for {Service}/{UserEmail}", service, userEmail);
        return Results.Problem(
            statusCode: 500,
            title: "Internal server error",
            detail: "An error occurred while creating the folder");
    }
})
.WithName("CreateFolder");

app.Run();

// Make the implicit Program class public so integration tests can access it
public partial class Program { }
