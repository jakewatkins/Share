# emailProcessor

emailProcessor is a c# .net console app that handles the processing of emails that have been categorized or classified.
emailProcessor is expected to run unattended

creates a new daily email report file stored in the DailyReportPath
    - the email report filename will have this format: email-report-yyyyMMdd.html
emailProcessor gets a list of json files from the ProcessedEmails path and processes each file:
    it loads the list of emails
    for each email:
        it gets the from address and category
        if the from is in the EmailWhiteList:
            if includeInReport is true
                Get a summary of the email and add the email to the daily email report

            if there is a handlerUrl in the EmailWhiteList record
                post the email to the the URL
        else
            get the handlerUrl from the CategoryHandler using the email's classification
                if the url is not null
                    post the email to the URL

    After all of the emails in the file have been processed
        move the processed email file to the email archive.  Change the file extension to .bak

if an error or exception occurs print a help error message and exit

## Project
    use .net 9
    get dependencies from nuget
    put the project in the emailProcessor directory
    add the project to the emailAgent.sln
    setup launch.json so the project can be debugged

## Implementation

### HandlerService
    Create a handler service that manages posting to the handlers
        post the entire email entity json in the body to the handler
        - add "API-KEY" to the http headers
        - add content type as application/json to the http headers
        - use standard http timeout
    the handler service will provide error handling
        - if an error occurs when posting to a handler - print an error message that providers information so the issue can be resolved include:
            - The HTTP status code and response body?
            - The email ID that failed?
            - The handler URL that was called?
        - if an error occurs the email processor will exit
    the service does not need to implement retry logic at this time

### SummaryService
    I have an LLM cli service that can be used to summarize emails.
    the LLMPath setting is the path to where the llm command lives
    to call the LLM the command line looks like:
        {LLMPath}\llm -PF {Prompt File}  -o {outputfile} --no-tools ibm-granite/granite-4.0-1b
        use path.combine to join llm with LLMPath to get a valid path on any platform
            Path.Combine(LLMPath, "llm");
    the outputfile is a random filename and placed in the system's temporary directory
    summaryPrompt is the prompt read from the configuration file
    The application passes in the email to be summarized.
    The service removed html from the subject and body of the email. and then stores the cleaned email to a file.  the file will be like this:
        {summaryPrompt}
        subject: {subject}
        body: {body}
        Use HtmlAgilityPack to remove the html from the subject and body
    the file is placed in the system's temporary directory
    the service then runs the llm tool to create the outputfile
    the service will read the contents of the outputfile into a string
    the service will then delete the prompt file and the outputfile
    the service will return the contents of the outputfile to the caller


## Dependencies
- Microsoft.Extensions.Configuration to load configuration information from json files
- Microsoft.Extensions.Logging & Serilog to log to text files
- HtmlAgilityPack
- emailServices project in emailServices.csproj

## Condiguration

### settings.json
setting.json will contain the following settings:
- API-KEY
    string value contain the api-key that will be used with the handlers
- summaryPrompt
    prompt used to summarize the email
- LLMPath
    path to where the llm command is located
- DailyReportPath
    path to the folder where the daily email report is stored
- EmailTemp
    the path will be where temporary email files can be stored
- ProcessedEmails
    the path to where emails to be processed are stored.
- EmailArchive
    path to where emails are archived
- StorageAccount
    connection string to an Azure Storage account where the tables for emailWhiteList, CategoryHandler
- logging configuration
- SeriLog configuration

### EmailWhiteList
Azure table that stores a list of email addresses that are handled differently than others.
The table will have the following fields:
    - name
    - email
    - includeInReport
    - handlerUrl
Look up records using email
    Query will be PartitionKey eq 'DigitalValet' and email eq '{from}'

### CategoryHandler
Azure table that stores a list of URLs for endpoints that process categorized emails
The table will have the following fields:
    - category
    - handlerUrl
Look up using category (category==classification)
    Query will be PartitionKey eq 'DigitalValet' and category eq '{classification}'

### Azure table
Use "DigitalValet" as the partition key in both tables (EmailWhiteList and CategoryHandler)
Use a guid for the RowKey in both tables (EmailWhiteList and CategoryHandler)
PartitionKey eq 'DigitalValet' and email eq '{from}' for the EmailWhiteList query
PartitionKey eq 'DigitalValet' and category eq '{classification}' for the CategoryHandler query

## Entities
Email entity:
    [
        {
            "id": "",
            "service": "",
            "from": "",
            "to": [
            ""
            ],
            "cc": [],
            "bcc": [],
            "sentDateTime": "",
            "subject": "",
            "body": "",
            "attachments": [],
            "classification": ""
        },
        ....
    ]
    this is the json structure of the email entity that will be being processed



  ### Daily report format
  the daily email report will be a simple html file with an html table.
  there will be no CSS at this time.
  the table will have the following columns:
  Email Subject
    - this will be an anchor tag with a URL to the email and the emails subject tag
        - the URL for the emails will be based on the service field
        - gmail: https://mail.google.com/mail/u/0/#inbox/{id}
        - outlook: https://outlook.live.com/mail/0/inbox/id/{id}

  Email summary
    - put the email summary here


## Clarifications
- it does not matter what order files are processed
- there will only be classified email files in the ProcessedEmails directory
- the file names already have timestamps so there won't be any duplicates.
    - if there is already a file with the same name in the archive - delete the file in the archive before moving the file that was processed
- if the daily report file already exists: delete it before starting to process emails
- if the from is in the EmailWhiteList then it does not need to be processed by the CategoryHandler part
- the EmailWhiteList has an optional field for a handler URL.  If there is a handler the email gets posted to that handler
- the LLM Path will just have the path to where the llm application is installed
- if the app errors out:
     - delete the prompt file and output file in the temp directory if they exist
     - Use a finally block of the SummaryService method to make sure these are always cleaned up

- use append mode to generate the report
- if there are no emails to process just make an empty report with just the empty html table, column headers and no rows. like this:
    <table>
        <tr><th>Email Subject</th><th>Email Summary</th></tr>
        <tr><td></td><td></td></tr>
    </table>
