namespace EmailAgent;

using System.Collections.Generic;

public class EmailAccount
{
    public string Type { get; set; } = string.Empty;
    public string Mailbox { get; set; } = string.Empty;
    public bool Enabled { get; set; } = false;
}
