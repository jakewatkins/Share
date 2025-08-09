using EmailAgent.Core;
using EmailAgent.Entities;
using EmailAgent.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;

namespace EmailAgent.Examples
{
    /// <summary>
    /// Example demonstrating how to use the Gmail service to retrieve emails
    /// </summary>
    public class GmailServiceExample
    {
        public static async Task Main(string[] args)
        {
            // Configure Serilog
            Log.Logger = new LoggerConfiguration()
                .WriteTo.File("gmail-example.log")
                .CreateLogger();

            try
            {
                // Build configuration
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("settings.json", optional: false)
                    .Build();

                // Create logger factory
                using var loggerFactory = LoggerFactory.Create(builder =>
                    builder.AddConsole());

                var logger = loggerFactory.CreateLogger<GmailService>();

                // Initialize configuration loader
                var agentConfig = new AgentConfiguration(configuration);

                // Create Gmail service
                var gmailService = new GmailService(agentConfig, logger);

                // Create email request
                var request = new GetEmailRequest
                {
                    NumberOfEmails = 10  // Retrieve 10 most recent emails
                };

                Console.WriteLine("Retrieving emails from Gmail...");

                // Get emails
                var response = await gmailService.GetEmail(request);

                if (response.Success)
                {
                    Console.WriteLine($"Successfully retrieved {response.Emails.Count} emails:");
                    Console.WriteLine();

                    foreach (var email in response.Emails)
                    {
                        Console.WriteLine($"Email ID: {email.Id}");
                        Console.WriteLine($"From: {email.From}");
                        Console.WriteLine($"To: {string.Join(", ", email.To)}");
                        
                        if (email.CC.Any())
                            Console.WriteLine($"CC: {string.Join(", ", email.CC)}");
                            
                        Console.WriteLine($"Subject: {email.Subject}");
                        Console.WriteLine($"Sent: {email.SentDateTime:yyyy-MM-dd HH:mm:ss}");
                        Console.WriteLine($"Service: {email.Service}");
                        
                        if (email.Attachments.Any())
                        {
                            Console.WriteLine($"Attachments ({email.Attachments.Count}):");
                            foreach (var attachment in email.Attachments)
                            {
                                Console.WriteLine($"  - {attachment.Name} ({attachment.Type}, {attachment.Size} bytes)");
                            }
                        }
                        
                        // Show first 100 characters of body
                        var bodyPreview = email.Body.Length > 100 
                            ? email.Body.Substring(0, 100) + "..." 
                            : email.Body;
                        Console.WriteLine($"Body Preview: {bodyPreview}");
                        Console.WriteLine(new string('-', 50));
                    }
                    
                    Console.WriteLine($"Total emails processed: {response.Count}");
                }
                else
                {
                    Console.WriteLine($"Failed to retrieve emails: {response.Message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Log.Error(ex, "An error occurred while running the Gmail service example");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}
