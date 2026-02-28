# Email Agent

Add the EmailAgent project to the existing emailAgent.sln
Email Agent is the email system for my digital valet system. Email Agent will use the emailServices library to get email from Gmail and outlook.com, this version will only support those 2 email providers.  Documentation for emailServices can be found in emailServices/README.md.
Email agent will perform the following tasks each time it runs:
Phase 1 of email agent:
- Get the list of email accounts to fetch email from
    - the description of the email configuration can be found below in the section "json configuration"
- For each email account perform the following:
    - for google email accounts switch emailServices GoogleId in the AgentConfiguration instance with the mailbox value for the account.  ClientId & ClientSecret do not change
    - for now there is only 1 outlook account so the existing clientId and secret are suffecient.  We'll deal with changes in the future.
    - retrieve all email in the mailbox in the default 'inbox'
        - we do not need to retrieve email from folders in the mailbox.
        - retrieve all email read and unread
        - Day to day email volume is low, less than 100 emails a day per account
    - save the emails in a json file 
        - use the TempFolder path from settings to store the email files
        - use a time and date stamp for the file name
            - append the date and time in the following format "yyyyMMddhhmmss" to the file name
        - name the file after the email account
        - for example:
            - jakew@guerillaprogrammer.com will go in jakewguerillaprogrammer-202601301300.json
            - runsincirclesscreaming@msn.com will go in runsincirclesscreamingmsn-202601301300.json
            - jake.watkins@commonspirit.org will go in jakewatkinscommonspirit--202601301300.json
            - jake.watkins@gmail.com will go in jakewatkinsgmail-202601301300.json
    - move all of the email in the account into an archive email folder (need to add a function to emailServices)
        - for now we'll hardcode the archive folder to be 'archive'. It will already exist prior to running the emailAgent

In Phase 2 (not relevant yet, for context only) we will implement this part of the process:
- after all of the email has been downloaded
    - make a zip file containing all of the download email files and move it to an archive (need to add a setting to the config)
    - create a new json file to save classified emails in (can go in the temp directory to start)
    - for each json file: 
        - load all of the emails in the file
            - for each email
                - call the email classifier LLM to classify the email (see [email classifications] section below for the classifications that will be used)
                - add a classification field to the email with the email's classification (we'll create a ClassifiedEmail entity based on the emailServices email entity)
                - save the classified email in the classified emails file
- that's it.

# Error Handling
If an error occurs (an exception) log an error and exit.
Do not worry about continuing to other accounts.. Just exit.

# Requirement 1 - Base application
The base Email Agent is a .net 9 console application.  It will use the following nuget packages:
    - Azure.Identity
    - Azure.Security.KeyVault.Secrets
    - Microsoft.Extensions.Configuration
    - Microsoft.Extensions.Configuration.Abstractions
    - Microsoft.Extensions.Configuration.Json
    - Microsoft.Extensions.Logging
    - Microsoft.Extensions.Logging.Abstratitions
    - Microsoft.Extensions.Logging.Console
    - Serilog
    - Serilog.Settings.Configuration
    - Serilog.Sinks.File
    - Google.Apis.Auth
    - Google.Apis.Gmail.v1
    - Microsoft.Graph
    - Microsoft.Graph.Auth
    - Microsoft.Identity.Client
    - System.Text.Json
the project will directly reference emailServices in its project file.
When the application starts, it will loads its configuration from seetings.json.  Setup and IConfiguration object that can be passed around during run time to configure services.  Also set up a logger base on the configuration in settings.json so we can log information to help with debugging.

# Requirement 2 - Outlook email


# Reference information

## scheduling
The Digital Valet is going to be run my the Mac OS/X's launchd scheduler.  The EmailAgent does not need to worry about implementing any scheduling.  It just fetches and classifies email.

## ClassifiedEmail entity
The ClassifiedEmail entity will add a field called "Classification" to the Email entity in the emailServices library (see emailServices/Entities/Email.cs).

## email classifications
promotional
transactional
notification
security
event
educational
newsletter
survey
business
personal
solicitation
recruitment
membership
political
informative
account
press
memorial
admission


## json configuration
{
    "EmailArchivePath": "/Users/jakewatkins/archive",
    "TempFolder": "/Users/jakewatkins/temp",
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
    ],
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