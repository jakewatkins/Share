using Microsoft.Extensions.Configuration;
using emailAgent;
using Serilog;

namespace TestApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Build configuration
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("settings.json", optional: false, reloadOnChange: true)
                .Build();

            try
            {
                // Create the outlook agent
                var agent = new outlookAgent(configuration);

                // Example usage: Get emails in batches
                int startIndex = 0;
                int batchSize = configuration.GetValue<int>("RetrievalCount", 50);
                bool hasMoreEmails = true;

                while (hasMoreEmails)
                {
                    Console.WriteLine($"Retrieving emails starting from index {startIndex}...");
                    
                    var response = await agent.GetEmail(startIndex, batchSize);
                    
                    if (response.Success)
                    {
                        Console.WriteLine($"Retrieved {response.Count} emails. Message: {response.Message}");
                        
                        foreach (var email in response.Emails)
                        {
                            Console.WriteLine($"  - {email.SentDateTime:yyyy-MM-dd HH:mm} | {email.From} | {email.Subject}");
                            if (email.Attachments.Any())
                            {
                                Console.WriteLine($"    Attachments: {string.Join(", ", email.Attachments.Select(a => $"{a.Name} ({a.Size} bytes)"))}");
                            }
                        }
                        
                        // Check if we've reached the end
                        if (response.Message == "Last" || response.Count < batchSize)
                        {
                            hasMoreEmails = false;
                            Console.WriteLine("Reached the end of the mailbox.");
                        }
                        else
                        {
                            startIndex += response.Count;
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Error: {response.Message}");
                        hasMoreEmails = false;
                    }
                    
                    // Add a small delay between batches
                    await Task.Delay(1000);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}
