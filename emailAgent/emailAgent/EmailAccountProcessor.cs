

using EmailAgent.Core;
using EmailAgent.Entities;
using EmailAgent.Services;
using Microsoft.Extensions.Logging;

namespace EmailAgent;

class EmailAccountProcessor
{
    enum EmailService
    {
        Outlook,
        Gmail,
        Owa
    }

    private readonly AgentConfiguration _config;
    private readonly ILogger<EmailAccountProcessor> _logger;

    public EmailAccountProcessor(AgentConfiguration config, ILogger<EmailAccountProcessor> logger)
    {
        _config = config;
        _logger = logger;
    }

    private async Task<List<Email>> GetOutlookEmails(EmailAccount account)
    {
        _logger.LogInformation("Retrieving emails for Outlook account: {Mailbox}", account.Mailbox);
        // Implementation for retrieving Outlook emails goes here
        var outlookService = new OutlookService(_config, _logger);
        var emailRequest = new GetEmailRequest
        {
            StartIndex = 0,
            NumberOfEmails = 500,
            Folder = new EmailFolder("Inbox", FolderType.Inbox, EmailAgent.Entities.EmailService.Outlook)
        };
        var emails = await outlookService.GetEmail(emailRequest);

        var resultEmails = new List<Email>();
        var moreEmails = true;

        while (moreEmails)
        {
            var fetchedEmails = await outlookService.GetEmail(emailRequest);
            if (fetchedEmails.Count == 0)
            {
                moreEmails = false;
            }
            else
            {
                resultEmails.AddRange(fetchedEmails.Emails);
                emailRequest.StartIndex += fetchedEmails.Count;
            }
        }

        return resultEmails;
    }

    private async Task<List<Email>> GetGMailEmails(EmailAccount account)
    {
        _config.GoogleId = account.Mailbox;

        var gmailService = new GmailService(_config, Program.GetLogger<GmailService>());

        var request = new GetEmailRequest
        {
            StartIndex = 0,
            NumberOfEmails = 500
        };

        var moreEmails = true;
        var resultEmails = new List<Email>();
        while (moreEmails)
        {
            var response = await gmailService.GetEmail(request);
            if (0 == response.Count)
            {
                moreEmails = false;
            }

            if (0 != response.Emails.Count)
            {
                resultEmails.AddRange(response.Emails);
                request.StartIndex += response.Count;
            }
        }

        return resultEmails;
    }
    private EmailService GetEmailService(string accountType)
    {
        switch (accountType.ToUpper())
        {
            case "OUTLOOK":
                return EmailService.Outlook;
            case "GMAIL":
                return EmailService.Gmail;
            case "OWA":
                return EmailService.Owa;
            default:
                throw new ArgumentOutOfRangeException(nameof(accountType));
        }
    }

    public async Task<List<Email>> GetEmails(EmailAccount account)
    {
        if (true == account.Enabled)
        {
            switch (GetEmailService(account.Type))
            {
                case EmailService.Outlook:
                    _logger.LogInformation($"processing outlook account {account.Mailbox}");
                    var email = await GetOutlookEmails(account);
                    return email;
                case EmailService.Gmail:
                    _logger.LogInformation($"processing gmail account {account.Mailbox}");
                    return await GetGMailEmails(account);
                case EmailService.Owa:
                    _logger.LogInformation($"processing Owa account {account.Mailbox}");
                    return new List<Email>();
            }
        }
        return null;
    }
}
