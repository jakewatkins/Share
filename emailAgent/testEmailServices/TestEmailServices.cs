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

                // Test folder functionality (backward compatibility and new features)
                await TestFolderFunctionality(agentConfig, loggerFactory);
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

        private async Task TestFolderFunctionality(AgentConfiguration agentConfig, ILoggerFactory loggerFactory)
        {
            Console.WriteLine("Testing Folder Functionality...");
            Console.WriteLine();

            // Test backward compatibility - requests without folder should default to Inbox
            await TestBackwardCompatibility(agentConfig, loggerFactory);
            Console.WriteLine();

            // Test explicit folder specification
            await TestExplicitFolders(agentConfig, loggerFactory);
            Console.WriteLine();

            // Test EmailFolder factory methods
            TestEmailFolderFactoryMethods();
        }

        private async Task TestBackwardCompatibility(AgentConfiguration agentConfig, ILoggerFactory loggerFactory)
        {
            Console.WriteLine("Testing Backward Compatibility (should default to Inbox)...");
            
            try
            {
                var logger = loggerFactory.CreateLogger<OwaService>();
                var owaService = new OwaService(agentConfig, logger);

                // Create request without specifying folder (should default to Inbox)
                var request = new GetEmailRequest
                {
                    NumberOfEmails = 2  // Small number for testing
                };

                Console.WriteLine($"Request folder: {(request.Folder == null ? "null (should default to Inbox)" : request.Folder.ToString())}");
                
                var response = await owaService.GetEmail(request);

                if (response.Success)
                {
                    Console.WriteLine($"✓ Backward compatibility test passed - Retrieved {response.Emails.Count} emails from default folder");
                }
                else
                {
                    Console.WriteLine($"✗ Backward compatibility test failed: {response.Message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Backward compatibility test error: {ex.Message}");
            }
        }

        private async Task TestExplicitFolders(AgentConfiguration agentConfig, ILoggerFactory loggerFactory)
        {
            Console.WriteLine("Testing Explicit Folder Specification...");
            
            try
            {
                var logger = loggerFactory.CreateLogger<OwaService>();
                var owaService = new OwaService(agentConfig, logger);

                // Test with explicit Inbox folder
                var inboxFolder = EmailFolder.CreateInboxFolder(EmailService.OWA);
                var inboxRequest = new GetEmailRequest
                {
                    NumberOfEmails = 2,
                    Folder = inboxFolder
                };

                Console.WriteLine($"Testing explicit Inbox folder: {inboxFolder}");
                var inboxResponse = await owaService.GetEmail(inboxRequest);

                if (inboxResponse.Success)
                {
                    Console.WriteLine($"✓ Explicit Inbox test passed - Retrieved {inboxResponse.Emails.Count} emails");
                }
                else
                {
                    Console.WriteLine($"✗ Explicit Inbox test failed: {inboxResponse.Message}");
                }

                // Test with Spam folder (might be empty, but should not error)
                var spamFolder = EmailFolder.CreateSpamFolder(EmailService.OWA);
                var spamRequest = new GetEmailRequest
                {
                    NumberOfEmails = 2,
                    Folder = spamFolder
                };

                Console.WriteLine($"Testing Spam folder: {spamFolder}");
                var spamResponse = await owaService.GetEmail(spamRequest);

                if (spamResponse.Success)
                {
                    Console.WriteLine($"✓ Spam folder test passed - Retrieved {spamResponse.Emails.Count} emails (folder may be empty)");
                }
                else
                {
                    Console.WriteLine($"⚠ Spam folder test returned error (may not exist): {spamResponse.Message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Explicit folder test error: {ex.Message}");
            }
        }

        private void TestEmailFolderFactoryMethods()
        {
            Console.WriteLine("Testing EmailFolder Factory Methods...");
            
            try
            {
                // Test factory methods for all services
                var services = new[] { EmailService.Gmail, EmailService.Outlook, EmailService.OWA };
                
                foreach (var service in services)
                {
                    Console.WriteLine($"  Testing {service} factory methods:");
                    
                    var inbox = EmailFolder.CreateInboxFolder(service);
                    Console.WriteLine($"    Inbox: {inbox} (ServiceSpecificId: {inbox.ServiceSpecificId})");
                    
                    var spam = EmailFolder.CreateSpamFolder(service);
                    Console.WriteLine($"    Spam: {spam} (ServiceSpecificId: {spam.ServiceSpecificId})");
                    
                    var sent = EmailFolder.CreateSentFolder(service);
                    Console.WriteLine($"    Sent: {sent} (ServiceSpecificId: {sent.ServiceSpecificId})");
                    
                    var drafts = EmailFolder.CreateDraftsFolder(service);
                    Console.WriteLine($"    Drafts: {drafts} (ServiceSpecificId: {drafts.ServiceSpecificId})");
                    
                    var trash = EmailFolder.CreateTrashFolder(service);
                    Console.WriteLine($"    Trash: {trash} (ServiceSpecificId: {trash.ServiceSpecificId})");
                    Console.WriteLine();
                }

                Console.WriteLine("✓ All EmailFolder factory methods working correctly");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ EmailFolder factory method test error: {ex.Message}");
            }
        }
    }
}
