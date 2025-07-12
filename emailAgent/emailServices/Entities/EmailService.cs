using System.Text.Json.Serialization;

namespace EmailAgent.Entities;

/// <summary>
/// Enum to identify which email service retrieved the email
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EmailService
{
    Gmail,
    Outlook,
    OWA
}
