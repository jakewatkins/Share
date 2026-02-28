using System.Text.Json;
using EmailAgent.Entities;

namespace EmailProcessor.Models;

/// <summary>
/// Extended email entity with classification field for processing
/// </summary>
public class ProcessedEmail : Email
{
    /// <summary>
    /// Email classification/category assigned by the classification system
    /// </summary>
    public string Classification { get; set; } = string.Empty;

    /// <summary>
    /// Creates a ProcessedEmail from a JSON element
    /// </summary>
    /// <param name="jsonElement">The JSON element containing email data</param>
    /// <returns>ProcessedEmail instance</returns>
    public static ProcessedEmail FromJsonElement(JsonElement jsonElement)
    {
        var email = new ProcessedEmail();

        // Map all the standard Email properties
        if (jsonElement.TryGetProperty("id", out var idElement))
        {
            email.Id = idElement.GetString() ?? string.Empty;
        }

        if (jsonElement.TryGetProperty("service", out var serviceElement))
        {
            var serviceName = serviceElement.GetString() ?? string.Empty;
            email.Service = serviceName.ToLowerInvariant() switch
            {
                "gmail" => EmailService.Gmail,
                "outlook" => EmailService.Outlook,
                "owa" => EmailService.OWA,
                _ => EmailService.Gmail
            };
        }

        if (jsonElement.TryGetProperty("from", out var fromElement))
        {
            email.From = fromElement.GetString() ?? string.Empty;
        }

        if (jsonElement.TryGetProperty("to", out var toElement) && toElement.ValueKind == JsonValueKind.Array)
        {
            email.To = toElement.EnumerateArray().Select(e => e.GetString() ?? string.Empty).ToList();
        }

        if (jsonElement.TryGetProperty("cc", out var ccElement) && ccElement.ValueKind == JsonValueKind.Array)
        {
            email.CC = ccElement.EnumerateArray().Select(e => e.GetString() ?? string.Empty).ToList();
        }

        if (jsonElement.TryGetProperty("bcc", out var bccElement) && bccElement.ValueKind == JsonValueKind.Array)
        {
            email.BCC = bccElement.EnumerateArray().Select(e => e.GetString() ?? string.Empty).ToList();
        }

        if (jsonElement.TryGetProperty("sentDateTime", out var sentDateElement))
        {
            if (DateTime.TryParse(sentDateElement.GetString(), out var sentDate))
            {
                email.SentDateTime = sentDate;
            }
        }

        if (jsonElement.TryGetProperty("subject", out var subjectElement))
        {
            email.Subject = subjectElement.GetString() ?? string.Empty;
        }

        if (jsonElement.TryGetProperty("body", out var bodyElement))
        {
            email.Body = bodyElement.GetString() ?? string.Empty;
        }

        // Map the classification field
        if (jsonElement.TryGetProperty("classification", out var classificationElement))
        {
            email.Classification = classificationElement.GetString() ?? string.Empty;
        }

        // Note: Attachments are not currently mapped as they're complex objects
        // and not needed for the current processing requirements

        return email;
    }
}
