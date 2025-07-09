# GMail Agent
- coding will follow the guidelines provided in ../rules.md
- the gmailAgent is a c# libary to access emails stored in google mail
- the agent will be a .NET 8 class library
- agent uses Google's gmail api
- the class name will be gmailAgent and the namespace will be emailAgent
- a normal project file structure should be created.  the root folder will be named gmailAgent (it already exists and contains this file)
- the assembly name should be the same as the project name - gmailAgent
- for Oauth the needed values are stored in my azure keyvaul.  
    - the library can assume that it has already been authenticated with Azure 
        - for example, when running the test application I'll do az-login and select my subscription before hand.
        - use DefaultAzureCredential
    - the keyvault name is stored in the settings.json file
    - the client id is in the secret googleClientId
    - the client secret is in the secret googleClientSecret
    - the mailbox id is in the secret googleCalendarId
    - use the nuget library Azure.Security.KeyVault.Secrets to access the keyvault
- Settings.json will contain configuration information used by the agent
    - the application's components will directly use the IConfigureation interface to get settings
    - keyvaultName will store the name of the keyvault that holds secrets for the application
    - RetrievalCount is an integer that will be used to determine how many emails to be retrieved at a time
        - if this setting is missing a default value of 500 will be used
    - MaxAttachmentSize is an integer representing, in bytes, how big an attachment can be
        - if this setting is missing a default value of 1MB
    - serilog settings will be included in settings.json 
        - this should include the normal serilog settings along with the configuration for the file sink for logging
        - the log file can be place in the application's working directory
        - use reasonable default settings for all serilog settings
- The agent's constructor will take an IConfiguration instance and use it to get the configuration values
- The Email entity will have the following properties
    - EmailMessageID
        - put the google Message ID here
    - From email address
    - To email address collection
    - CC email address collection
    - BCC email address collection
    - Sent date and time
        - if the sent date and time are missing the internal date can be mapped here
    - Subject
    - HTML Message body
        - this will contain the message body in HTML format
    - Plain Message body
        - this will contain the message body in plain text format
    - Attachment collection
        - attachments will have a name, type, size, and content string that is base64 encoded
        - all types of attachments will be handled and no filtering is needed
        - If an attachment is larger than MaxAttachmentSize, the size field will be set, but the stream will not be downloaded and the content field set to null
- Do not worry about email threading
- Use serilog to log to a file for debugging, just use the filename in the settings.
- No retry logic is needed at this time.  
- The agent will not be interactive and is expected to take a long time to run, so performance is a secondary consideration.
- we'll not worry about being able to cancel operations
- For rate limiting we want to avoid calling the gmail api more than 1 time every second
- we won't need to worry about memory management in regard to attachments.

## GMail OAuth flow
- the client id and client secret will come from a Microsoft Azure Keyvault. Don't worry about authentication with azure.
- the client id and client secret will be used to perform OAuth authentication with Google Workspace to gain access to the GMail account.
- do not worry about token expiration

## GMail api scope
- we'll need to modify emails in the future
- we need to use https://www.googleapis.com/auth/gmail.modify API scope

## Main API
- The Agent's primary API will be GetEmail
    - the API will take 2 parameters
        - start index indicating which email in the inbox to start at
        - number of emails to fetch
    - The API will return an object with the following properties
        - Success, a boolean field indicating if the email agent was able to retrieve emails
        - Message, a string that will contain an error messages if Success is not true, otherwise it can just contain 'ok'
        - Count, an integer of how many emails were retrieved.
        - Emails a collection of email entities that have been retrieved from the mailbox
- Email will be retrieved oldest to newest
    - a start index of 0 means the oldest email in the mailbox
    - emails should be ordered using internalDate in the email message
- Email will only be retrieved from the inbox
- When the program calls the api the first time the first email index will be 0 and the count will be what have has been configured in settings.json
- for pagination we'll just use a simple index based approach for retrieving emails
- The agent won't worry about new emails arriving between calls to the API.  If an email is missed it won't matter
- The program will make additional calls to the API until the Count property is less than the RetrievalCount setting
- When the agent gets to the last email in the mail box it will set message to 'Last'
- If the agent is unable to access the mailbox for any reason it will set the Success field to false and put complete error details in the Message field so the issue can be debugged and resolved.
    - the stack trace can be left out of the error details
    - if there is an inner exception it's message should be included.  we only need to go 1 level deep.
    - do include error messages that will help the developer fix the problem


