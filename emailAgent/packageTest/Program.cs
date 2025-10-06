using EmailAgent.Entities;
using EmailAgent.Services;

namespace PackageTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Testing EmailAgent.emailServices package...");

            // Test creating entities
            var email = new Email
            {
                Id = "test-123",
                Service = EmailService.Gmail,
                From = "test@example.com",
                Subject = "Test Email",
                Body = "This is a test email body",
                SentDateTime = DateTime.Now
            };

            Console.WriteLine($"Created email: {email.Subject} from {email.From}");
            Console.WriteLine($"Email Service: {email.Service}");
            Console.WriteLine($"Email ID: {email.Id}");

            // Test creating an attachment
            var attachment = new EmailAttachment
            {
                Name = "test.txt",
                Type = "text/plain",
                Size = 1024,
                Content = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("Test content"))
            };

            Console.WriteLine($"Created attachment: {attachment.Name} ({attachment.Size} bytes)");

            // Test creating a request
            var request = new GetEmailRequest
            {
                StartIndex = 0,
                NumberOfEmails = 10
            };

            Console.WriteLine($"Created request: Start={request.StartIndex}, Count={request.NumberOfEmails}");

            Console.WriteLine("Package test completed successfully!");
        }
    }
}
