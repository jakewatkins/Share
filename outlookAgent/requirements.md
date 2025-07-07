# Outlook Agent
- the outlookAgent is a c# libary to access emails stored at outlook.com
- the agent will be a .NET standard library that uses .NET 8
- agent uses Microsoft Graph API
- the class name will be outlookAgent and the namespace will be emailAgent
- a normal project file structure should be created.  the root folder will be named outlookAgent (it already exists and contains this file)
- the assembly name should be the same as the project name - outlookAgent
- Settings.json will contain configuration information used by the agent
    - RetrievalCount is an integer that will be used to determine how many emails to be retrieved at a time
    - MaxAttachmentSize is an integer representing, in bytes, how big an attachment can be
    - serilog settings will be included in settings.json 
        - this should include the normal serilog settings along with the configuration for the file sink for logging
- The agent's constructor will take an IConfiguration instance and use it to get the configuration values
- The Email entity will have the following properties
    - From email address
    - To email address collection
    - Sent date and time
    - Subject
    - Message body
    - Attachment collection
    - attachments will have a name, type, size, and content string that is base64 encoded
    - If an attachment is larger than MaxAttachmentSize, the size field will be set, but the stream will not be downloaded
- Use serilog to log to a file for debugging
- No retry logic is needed at this time.  
- The agent will not be interactive and is expected to take a long time to run, so performance is a secondary consideration.
- we'll not worry about being able to cancel operations

## Application Registration
- to access Azure resources we'll use an application registration in Azure Entra ID
    - the application's clientId will stored as an environment variable called 'valetClientId'
    - the application's clientSecret will be stored as an environment variable called 'valetSecret'

## Main API
- The agent will use Deleegated permissions to access the user's mailbox stored in Office365
    - the first time the application runs the user will have to be present to grant access.
    - the application will store the user's token in a text file in the application's working directory
        - call the file usertoken.json
    - if the token file already exists, the application will renew the token and update the file with the refreshed token.
- When the agent connect to Office365 it will need the following permissions:   
    - Mail.Read
    - Mail.ReadWrite
- The Agent's primary API will be GetEmail
    - the API will take 2 parameters
        - start index indicating which email in the inbox to start at
        - number of emails to fetch
    - The API will return an object with the following properties
        - Success, a boolean field indicating if the email agent was able to retrieve emails
        - Message, a string that will contain an error messages if Success is not true, otherwise it can just contain 'ok'
        - Count, an integer of how many emails were retrieved.
        - Emails a collection fo email entities that have been retrieved from the mailbox
- Email will be retrieved oldest to newest
    - a start index of 0 means the oldest email in the mailbox
    - emails should be ordered using ReceivedDateTime
- Email will only be retrieved from the inbox
- When the program calls the api the first time the first email index will be 0 and the count will be what have has been configured in settings.json
- for pagination we'll just use a simple index based approach for retrieving emails
- The agent won't worry about new emails arriving between calls to the API.  If an email is missed it won't matter
- The program will make additional calls to the API until the Count property is less than the RetrievalCount setting
- When the agent gets to the last email in the mail box it will set message to 'Last'
- If the agent is unable to access the mailbox for any reason it will set the Success field to false and put complete error details in the Message field so the issue can be debugged and resolved.
    - the stack trace can be left out of the error details
    - do include error messages that will help the developer fix the problem


