namespace emailAgent;

public class EmailAttachment
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public long Size { get; set; }
    public string? Content { get; set; }
}
