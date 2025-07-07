namespace emailAgent
{
    public class Email
    {
        public string From { get; set; } = string.Empty;
        public List<string> To { get; set; } = new List<string>();
        public DateTime SentDateTime { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string MessageBody { get; set; } = string.Empty;
        public List<EmailAttachment> Attachments { get; set; } = new List<EmailAttachment>();
    }

    public class EmailAttachment
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public long Size { get; set; }
        public string Content { get; set; } = string.Empty; // Base64 encoded content
    }

    public class GetEmailResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int Count { get; set; }
        public List<Email> Emails { get; set; } = new List<Email>();
    }
}
