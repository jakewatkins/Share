using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using EmailAgent.Core;
using EmailAgent.Services;
using EmailAgent.Entities;

namespace EmailAgent.Examples;

/// <summary>
/// Example usage of the OwaService
/// </summary>
public class OwaServiceExample
{
    /// <summary>
    /// Demonstrates how to use the OwaService to retrieve emails
    /// </summary>
    public static async Task RunExample()
    {
        // Setup configuration
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("settings.json")
            .Build();

        // Setup logging
        using var loggerFactory = LoggerFactory.Create(builder =>
            builder.AddConsole().SetMinimumLevel(LogLevel.Information));
        var logger = loggerFactory.CreateLogger<OwaServiceExample>();

        try
        {
            // Initialize agent configuration
            var agentConfig = new AgentConfiguration(configuration);
            logger.LogInformation("AgentConfiguration initialized successfully");

            // Initialize OWA service
            var owaService = new OwaService(agentConfig, logger);
            logger.LogInformation("OwaService initialized successfully");

            // Create email request
            var request = new GetEmailRequest
            {
                NumberOfEmails = 5  // Retrieve 5 oldest emails
            };

            logger.LogInformation("Retrieving {EmailCount} emails from OWA", request.NumberOfEmails);

            // Retrieve emails
            var response = await owaService.GetEmail(request);

            // Process response
            if (response.Success)
            {
                logger.LogInformation("Successfully retrieved {EmailCount} emails", response.Count);

                foreach (var email in response.Emails)
                {
                    Console.WriteLine($"Email ID: {email.Id}");
                    Console.WriteLine($"Service: {email.Service}");
                    Console.WriteLine($"From: {email.From}");
                    Console.WriteLine($"Subject: {email.Subject}");
                    Console.WriteLine($"Sent: {email.SentDateTime:yyyy-MM-dd HH:mm:ss}");
                    Console.WriteLine($"To: {string.Join(", ", email.To)}");
                    
                    if (email.CC.Any())
                        Console.WriteLine($"CC: {string.Join(", ", email.CC)}");
                    
                    Console.WriteLine($"Body Length: {email.Body.Length} characters");
                    Console.WriteLine($"Attachments: {email.Attachments.Count}");

                    // Display attachment details
                    foreach (var attachment in email.Attachments)
                    {
                        Console.WriteLine($"  - {attachment.Name} ({attachment.Type}, {attachment.Size} bytes)");
                    }

                    Console.WriteLine(new string('-', 50));
                }
            }
            else
            {
                logger.LogError("Failed to retrieve emails: {ErrorMessage}", response.Message);
                Console.WriteLine($"Error: {response.Message}");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while running the OWA example");
            Console.WriteLine($"Exception: {ex.Message}");
        }
    }
}
