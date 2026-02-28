# Email Agent Configuration

## overview
Email Agent will have to access multiple email accounts from several different services.  I need a way to provide configuraiton information so the agent will be able to find and access the mailboxes.
The existing emailServices library handles most of what is needed.  But there will be the situation where I have multiple gmail accounts that need to be accessed.  To configuraiton this we'll use the following JSON fragment for configuration:

    "EmailAccounts" : [
        {
            "type" : "gmail",
            "mailbox": "jake.watkins@gmail.com"

        },
        {
            "type" : "gmail",
            "mailbox": "jakew@guerillaprogrammer.com"
        },
        {
            "type" : "gmail",
            "mailbox": "jake.watkins@commonspirit.org"
        },
        {
            "type" : "outlook",
            "mailbox": "RunsInCirclesScreaming@msn.com"
        }
    ]
We'll just add this to the existing settings file:
{
    "keyvaultName": "your-keyvault-name",
    "RetrievalCount": 500,
    "MaxAttachmentSize": 1048576,
    "EmailAccounts" : [
        {
            "type" : "gmail",
            "mailbox": "jake.watkins@gmail.com"

        },
        {
            "type" : "gmail",
            "mailbox": "jakew@guerillaprogrammer.com"
        },
        {
            "type" : "gmail",
            "mailbox": "jake.watkins@commonspirit.org"
        },
        {
            "type" : "outlook",
            "mailbox": "RunsInCirclesScreaming@msn.com"
        }
    ]
    "Serilog": {
        "Using": [ "Serilog.Sinks.File" ],
        "MinimumLevel": {
        "Default": "Information",
        "Override": {
            "Microsoft": "Warning",
            "System": "Warning"
        }
        },
        "WriteTo": [
        {
            "Name": "File",
            "Args": {
            "path": "emailServices.log",
            "rollingInterval": "Day",
            "retainedFileCountLimit": 7,
            "outputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
            }
        }
        ]
    }
}

we'll need to add the following settings to the settings file:
    - EmailArchivePath
    - 