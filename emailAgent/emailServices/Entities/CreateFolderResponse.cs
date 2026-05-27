namespace EmailAgent.Entities;

public class CreateFolderResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string FolderName { get; set; } = string.Empty;
    public string? ServiceSpecificId { get; set; }
    public EmailService Service { get; set; }
}
