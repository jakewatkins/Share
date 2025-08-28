using EmailAgent.Core;
using EmailAgent.Entities;
using EmailAgent.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace testEmailServices
{
    public class TestEmailServices
    {
        private readonly IConfiguration _configuration;

        public TestEmailServices(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public async Task Run()
        {
            Console.WriteLine("=== Starting Email Services Test ===");
            Console.WriteLine();

            // Create logger factory
            using var loggerFactory = LoggerFactory.Create(builder =>
                builder.AddConsole());

            try
            {
                Console.WriteLine("Loading configuration...");
                
                // Initialize configuration loader
                var agentConfig = new AgentConfiguration(_configuration);
                Console.WriteLine("Configuration loaded successfully");
                Console.WriteLine();

                // Test OWA Service
                await TestOwaService(agentConfig, loggerFactory);
                Console.WriteLine();

                // Test Gmail Service
                //await TestGmailService(agentConfig, loggerFactory);
                Console.WriteLine();

                // Test Outlook Service
                //await TestOutlookService(agentConfig, loggerFactory);
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during test execution: {ex.Message}");
            }

            Console.WriteLine("=== Email Services Test Completed ===");
        }

        private async Task TestOwaService(AgentConfiguration agentConfig, ILoggerFactory loggerFactory)
        {
            Console.WriteLine("Testing OWA Service...");
            
            try
            {
                var logger = loggerFactory.CreateLogger<OwaService>();
                var owaService = new OwaService(agentConfig, logger);

                var request = new GetEmailRequest
                {
                    NumberOfEmails = 5
                };

                var response = await owaService.GetEmail(request);

                if (response.Success)
                {
                    Console.WriteLine($"OWA Service: Retrieved {response.Emails.Count} emails");
                    
                    foreach (var email in response.Emails)
                    {
                        Console.WriteLine($"{email.SentDateTime:yyyy-MM-dd HH:mm:ss} - {email.Service} - {email.From} - {email.Subject}");
                    }
                }
                else
                {
                    Console.WriteLine($"OWA Service failed: {response.Message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"OWA Service error: {ex.Message}");
            }
        }

        private async Task TestGmailService(AgentConfiguration agentConfig, ILoggerFactory loggerFactory)
        {
            Console.WriteLine("Testing Gmail Service...");
            
            try
            {
                var logger = loggerFactory.CreateLogger<GmailService>();
                var gmailService = new GmailService(agentConfig, logger);

                var request = new GetEmailRequest
                {
                    NumberOfEmails = 5
                };

                var response = await gmailService.GetEmail(request);

                if (response.Success)
                {
                    Console.WriteLine($"Gmail Service: Retrieved {response.Emails.Count} emails");
                    
                    foreach (var email in response.Emails)
                    {
                        Console.WriteLine($"{email.SentDateTime:yyyy-MM-dd HH:mm:ss} - {email.Service} - {email.From} - {email.Subject}");
                    }
                }
                else
                {
                    Console.WriteLine($"Gmail Service failed: {response.Message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Gmail Service error: {ex.Message}");
            }
        }

        private async Task TestOutlookService(AgentConfiguration agentConfig, ILoggerFactory loggerFactory)
        {
            Console.WriteLine("Testing Outlook Service...");
            
            try
            {
                var logger = loggerFactory.CreateLogger<OutlookService>();
                var outlookService = new OutlookService(agentConfig, logger);

                var request = new GetEmailRequest
                {
                    NumberOfEmails = 5
                };

                var response = await outlookService.GetEmail(request);

                if (response.Success)
                {
                    Console.WriteLine($"Outlook Service: Retrieved {response.Emails.Count} emails");
                    
                    foreach (var email in response.Emails)
                    {
                        Console.WriteLine($"{email.SentDateTime:yyyy-MM-dd HH:mm:ss} - {email.Service} - {email.From} - {email.Subject}");
                    }
                }
                else
                {
                    Console.WriteLine($"Outlook Service failed: {response.Message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Outlook Service error: {ex.Message}");
            }
        }
    }
}
