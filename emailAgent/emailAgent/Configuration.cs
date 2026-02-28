namespace EmailAgent;

/// <summary>
/// Represents the configuration for the Email Agent application
/// </summary>
public class EmailAgentConfiguration
{
    public string EmailArchivePath { get; set; } = string.Empty;
    public string TempFolder { get; set; } = string.Empty;
    public string KeyvaultName { get; set; } = string.Empty;
    public int RetrievalCount { get; set; }
    public int MaxAttachmentSize { get; set; }
    public List<EmailAccount> EmailAccounts { get; set; } = new();
    public Dictionary<string, string> DebugSecrets { get; set; } = new();
}
