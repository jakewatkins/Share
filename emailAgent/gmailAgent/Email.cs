namespace emailAgent;

public class Email
{
    public string EmailMessageID { get; set; } = string.Empty;
    public string From { get; set; } = string.Empty;
    public List<string> To { get; set; } = new();
    public List<string> CC { get; set; } = new();
    public List<string> BCC { get; set; } = new();
    public DateTime SentDateTime { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string HtmlMessageBody { get; set; } = string.Empty;
    public string PlainMessageBody { get; set; } = string.Empty;
    public List<EmailAttachment> Attachments { get; set; } = new();
}
