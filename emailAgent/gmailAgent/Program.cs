using emailAgent;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace gmailAgent.Test;

class Program
{
    static async Task Main(string[] args)
    {
        // Build configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("settings.json", optional: false, reloadOnChange: true)
            .AddJsonFile("settings.development.json", optional: true, reloadOnChange: true)
            .Build();

        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .CreateLogger();

        try
        {
            Log.Information("Gmail Agent Test Started");

            // Create Gmail agent
            var agent = new emailAgent.gmailAgent(configuration);

            // Test retrieving emails
            var result = await agent.GetEmail(0, 10);

            if (result.Success)
            {
                Console.WriteLine($"Successfully retrieved {result.Count} emails");
                Console.WriteLine($"Message: {result.Message}");
                
                foreach (var email in result.Emails)
                {
                    Console.WriteLine($"From: {email.From}");
                    Console.WriteLine($"Subject: {email.Subject}");
                    Console.WriteLine($"Date: {email.SentDateTime}");
                    Console.WriteLine($"Attachments: {email.Attachments.Count}");
                    Console.WriteLine("---");
                }
            }
            else
            {
                Console.WriteLine($"Failed to retrieve emails: {result.Message}");
            }
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
