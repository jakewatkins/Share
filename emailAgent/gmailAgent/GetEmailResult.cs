namespace emailAgent;

public class GetEmailResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int Count { get; set; }
    public List<Email> Emails { get; set; } = new();
}
