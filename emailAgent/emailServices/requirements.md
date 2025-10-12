# EmailAgent.emailServices
- this is a .net 8.0 library that will be share
- coding will follow the guidelines provided in ../rules.md
- emailServices library provides email services to other applications so they are able to fetch and process email.
- There will be 3 separate services in the libary:
    - a service to fetch email from google's gmail service
    - a service to fetch email from Microsoft's Outlook email service
    - a service to fetch email from Microsoft's OWA api
- there will be entities shared across services to represent different parts of emails: 
    - an email entity
    - an attachement entity
    - other entities as required
- the library will use Microsoft .net configuration to load settings
- some setting will be stored in a Microsoft Azure Key Vault
    - the library does not need to worry about autheticating with Microsoft Azure in order to access the Key Vault.
    - the calling application will be responsible for authenticating with Microsoft Azure.
- Settings.json will contain configuration information used by the email services in the library
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
- Use serilog to log to a file for debugging, just use the filename in the settings.
- for configuration purposes, we pass the IConfiguration interface to classes
- if logging errors, always use the error log level

# Requirement 1 - email entities
- use EmailAgent.Entities as the namespace for entities
- put the entities in their own folder in the project
- review the other three project in the solution (gmailAgent, outlookAgent, owaAgent)
- create entities to represent email and attachments that will meet the needs of the services we will create in the future
- the entities will be POCOs and should not contain any business logic
- the entities should be serializable to json
- In general we want to avoid duplicating properties just because services call things differently.
- In the case of the email body, we will only have one body.
    - in the case of gmail we will favor the HTML body of plaintext
- We do want to be able to differentiate which mailbox each email came from so adding an email service indicator (an enum for example) would be a good idea.
- we will want to use the Request/Response entity pattern used in the gmailAgent
    - the entities will be shared across services (everybody uses the same one)

# Requirement 2 - Configuration loader
- use EmailAgent.Core as the namespace
- put the AgentConfiguration class in the Core folder
- the constructor for AgentConfiguration will take an IConfiguration entity
- the class will load configuration settings from the Azure Key Vault so the services don't have to deal with the key vault
- the class will load everything from the key vault as a part of its construction
    - implement the constructor so it is not asynchronous.  Use await or some other approach to wait for completion of operations.
- each of the keyvault secrets will be represented as public properties on the class:
    - owaServiceURI
    - owaPassword
    - owaEmailAddress
    - googleCalendarId
    - googleClientId
    - googleClientSecret
    - outlookClientId
    - outlookSecret
- have properties for general email agent configuration
    - keyvaultName
    - RetrievalCount (positive integer)
    - MaxAttachmentSize (positive integer)
 - if the service is unable to retrieve secrets from the keyvault an exception should be thrown
 - if keyvaultName is missing from settings.json an exception should be thrown
 - if the values are missing from settings.json use default values:
    - RetrievalCount = 500
    - MaxAttachmentSize = 1048576
 - we don't need to worry about other configuration stuff (ie Serilog)

# Requirement 3 - OWA Service 
- use EmailAgent.Services as the namespace
- put the OwaService class in the service folder
- No interface will be needed for this class
- Create the OWA Service base on the code in the owaAgent project
- Use Microsoft.Exchange.WebServices nuget library to access the OWA API
    - Use WebCredentails to authenticate with the service
    - if authetication fails just throw an exception
- the constructor will take an instance of AgentConfiguration to get to the configuration information
    - owaServiceURI
    - owaPassword
    - owaEmailAddress
- if the configuration values are missing, throw an exception
- ILogger will be passed to the constructor and should be saved as a private field to be used
- no retry logic is needed, if a call fails: throw an exception and provide information so it can be debugged
- the main method will be GetEmail
    - await all asynchronous calls so clients do not have to be multithreaded
- Use the GetEmailRequest entity as the input for retrieving emails 
- Retrieve the oldest emails first
- Leave emails marked unread
- Only retrieve information about attachments
    - file name
    - file type (jpg, pdf, doc, etc)
    - attachment size in bytes
- use the NumberOfEmails to control how many emails to retrieve
- map the retrieved emails to Entities.Email and add them to GetEmailResponse.Emails collection
    - Do not worry about other fields in the EWS email entity
    - if an email has multiple email formats, favor HTML over plain text
- if there is an error put the error information the the response object

# Requirement 4 - outlook Service
- use EmailAgent.Services as the namespace
- OutlookService will use Microsoft's Graph SDK so those nuget packages will need to be added to the project
- put the OutlookService class in the service folder
- No interface will be needed for this class
- Create the Outlook Service base on the code in the outlookAgent project (../outlookAgent/outlookAgent.csproj)
- the constructor will take an instance of AgentConfiguration to get to the configuration information
    - outlookClientId
    - outlookSecret
    - both values will come from the AgentConfiguration object
- if the configuration values are missing, throw an exception
- ILogger will be passed to the constructor and should be saved as a private field to be used
- no retry logic is needed, if a call fails: throw an exception and provide information so it can be debugged
- to authenticate with Microsoft's outlook service use the PublicClientApplicationBuilder 
    - an example of this can be seen in ../outlookAgent/outlookAgent.cs lines 48 to 59.
    - the scopes will be "Mail.Read", "Mail.ReadWrite"
- the main method will be GetEmail
    - await all asynchronous calls so clients do not have to be multithreaded
- Use the GetEmailRequest entity as the input for retrieving emails 
- Retrieve the oldest emails first
- Leave emails marked unread
- Only retrieve information about attachments
    - file name
    - file type (jpg, pdf, doc, etc)
    - attachment size in bytes
- use the NumberOfEmails to control how many emails to retrieve
- map the retrieved emails to Entities.Email and add them to GetEmailResponse.Emails collection
    - Do not worry about other fields in the Graph.Message entity
    - if an email has multiple email formats, favor HTML over plain text
- if there is an error put the error information the the response object

# Requirement 5 gmail Service
- use EmailAgent.Services as the namespace
- GmailService will use Google's Google.Apis.Gmail.v1 and related packages so those nuget packages will need to be added to the project
- put the GmailService class in the service folder
- No interface will be needed for this class
- Create the GMail Service based on the code in the gmailAgent project (../gmailAgent/gmailAgent.csproj)
- the constructor will take an instance of AgentConfiguration to get to the configuration information
    - GoogleCalendarId
        - this is actually the email address, but b/c it is shared by an earlier service we'll continue using it as is.
    - GoogleClientId
    - GoogleClientSecret
    - the values will come from the AgentConfiguration object
- if the configuration values are missing, throw an exception
- ILogger will be passed to the constructor and should be saved as a private field to be used
- no retry logic is needed, if a call fails: throw an exception and provide information so it can be debugged
- to authenticate with Google's email service use the GoogleWebAuthorizationBroker 
    - an example of this can be seen in ../gmailAgent/gmailAgent.cs lines 129 to 155.
        - Please review the code in ../gmailAgent/gmailAgent.cs to see how I've done this previously.
    - We want GmailService.Scope.GmailModify permission, falling back to GmailReadonly is acceptable
- the main method will be GetEmail
    - await all asynchronous calls so clients do not have to be multithreaded
- Use the GetEmailRequest entity as the input for retrieving emails 
- To retrieve emails from gmail, see my code in GetInboxMessages found in ../gmailAgent/gmailAgent.cs the method starts at line 160
- Leave emails marked unread
- Only retrieve information about attachments
    - file name
    - file type (jpg, pdf, doc, etc)
    - attachment size in bytes
- use the NumberOfEmails to control how many emails to retrieve
- map the retrieved emails to Entities.Email and add them to GetEmailResponse.Emails collection
    - Do not worry about other fields in the Gmail Message entity
    - if an email has multiple email formats, favor HTML over plain text
    - you can follow my code in ../gmailAgent/gmailAgent.cs method ParseMessagePart starting at line 265 which calls GetMessagePartContent starting at line 300.
- if there is an error put the error information the the response object

# Requirement 6 - delete OWA email
- I want a method called DeleteEmail added to OwaService
- the user will pass in an Email entity to the method and the method will delete it
- the email signature will look like:
    public bool DeleteEmail(Email email)
- the method will delete 1 email at a time.
- the method will verify that the email entity has a service type of OWA and that the id value is not empty or null
    - if it is not of service type OWA, log the validation failure (email id and wrong service) and return false
    - if the id value is null or empty, log the validation failure (missing email id) and return false
- the method will use the Email entity's id value to call the OWA service's DeleteItems
- refactor the existing code as necassary to minimize code duplication
    - for example - move creating the server connection to a private shared method
    - cache the connection so it can be reused across multiple calls.
    - the service should implement IDisposable so everything can be cleaned up with the service is destroyed.
    - if a connection to the server cannot be established thrown an exception
- if the service returns a ServiceResponseException, log it and throw an exception
- if the the service response ServiceError is not equal to 0, log the email's id, the ErrorDetails and ErrorMessage and then return false
- if the the service response ServiceError is equal to 0 return true.

# Requirement 7 - delete Gmail email
- I want a method called DeleteEmail added to GmailService
- the user will pass in an Email entity to the method and the method will delete it
- the email signature will look like:
    public bool DeleteEmail(Email email)
- the method will delete 1 email at a time.
- the method will verify that the email entity has a service type of Gmail and that the id value is not empty or null
    - if it is not of service type Gmail, log the validation failure (email id and wrong service) and return false
    - if the id value is null or empty, log the validation failure (missing email id) and return false
- the method will use the Email entity's id value to call the Gmail service's Messages.Delete method
- refactor the existing code as necassary to minimize code duplication
    - for example - move creating the Gmail service connection to a private shared method
    - cache the Gmail service connection so it can be reused across multiple calls.
    - the service should implement IDisposable so everything can be cleaned up when the service is destroyed.
    - if a connection to the Gmail service cannot be established throw an exception
- if the Gmail service throws a GoogleApiException, log it and throw an exception
- if the Gmail service delete operation completes successfully return true
- if the Gmail service returns any other error or exception, log the email's id and the error details and then return false

# Requirement 8 - delete Outlook email
- I want a method called DeleteEmail added to OutlookService
- the user will pass in an Email entity to the method and the method will delete it
- the email signature will look like:
    public bool DeleteEmail(Email email)
- the method will delete 1 email at a time.
- the method will verify that the email entity has a service type of Outlook and that the id value is not empty or null
    - if it is not of service type Outlook, log the validation failure (email id and wrong service) and return false
    - if the id value is null or empty, log the validation failure (missing email id) and return false
- the method will use the Email entity's id value to call the Microsoft Graph API's Messages.Delete method
- refactor the existing code as necassary to minimize code duplication
    - for example - move creating the Graph service client to a private shared method
    - cache the Graph service client so it can be reused across multiple calls.
    - the service should implement IDisposable so everything can be cleaned up when the service is destroyed.
    - if a connection to the Microsoft Graph service cannot be established throw an exception
- if the Microsoft Graph service throws a ServiceException, log it and throw an exception
- if the Microsoft Graph service delete operation completes successfully return true
- if the Microsoft Graph service returns any other error or exception, log the email's id and the error details and then return false

# Requirement 9 - Refactor email retrieval for folder and SPAM support
- This requirement prepares the email services for upcoming requirements to retrieve emails from specific folders and SPAM folders
- The current GetEmail methods are hardcoded to retrieve emails from the Inbox folder only
- We need to refactor the code to make folder selection flexible and reusable

## 9.1 - Create EmailFolder entity
- Create a new entity called EmailFolder in the EmailAgent.Entities namespace
- Properties should include:
    - FolderName (string) - Display name of the folder
    - FolderType (enum) - Inbox, Sent, Drafts, Spam, Trash, Custom
    - ServiceSpecificId (string) - Service-specific folder identifier (e.g., Gmail label, Outlook folder ID)
    - Service (EmailService) - Which service this folder belongs to

## 9.2 - Update GetEmailRequest entity
- Add an optional EmailFolder property to GetEmailRequest
- If EmailFolder is null or not provided, default to Inbox behavior (maintains backward compatibility)
- Add validation to ensure the EmailFolder.Service matches the service being called

## 9.3 - Refactor OwaService for folder support
- Extract folder-specific logic from GetEmail method into a private method: GetEmailsFromFolder(EmailFolder folder, GetEmailRequest request)
- Create a private method: ResolveFolderIdFromEmailFolder(EmailFolder emailFolder) that maps EmailFolder to EWS FolderId
- Support common folder types: Inbox, Sent, Drafts, Spam (Junk Email), Trash (Deleted Items)
- For custom folders, use the ServiceSpecificId as the folder path/name
- Update GetEmail method to call GetEmailsFromFolder with Inbox as default folder
- Ensure the refactored code reuses existing connection management and email mapping logic

## 9.4 - Refactor GmailService for folder support  
- Extract folder-specific logic from GetEmail method into a private method: GetEmailsFromFolder(EmailFolder folder, GetEmailRequest request)
- Create a private method: ResolveGmailQueryFromEmailFolder(EmailFolder emailFolder) that maps EmailFolder to Gmail search query
- Support common folder types using Gmail labels/queries:
    - Inbox: "in:inbox"
    - Sent: "in:sent" 
    - Drafts: "in:drafts"
    - Spam: "in:spam"
    - Trash: "in:trash"
    - Custom: use ServiceSpecificId as Gmail label name
- Update GetInboxMessages method name to GetMessagesFromFolder and accept folder parameter
- Update GetEmail method to call GetEmailsFromFolder with Inbox as default folder
- Ensure the refactored code reuses existing connection management, rate limiting, and email mapping logic

## 9.5 - Refactor OutlookService for folder support
- Extract folder-specific logic from GetEmail method into a private method: GetEmailsFromFolder(EmailFolder folder, GetEmailRequest request)
- Create a private method: ResolveGraphFolderFromEmailFolder(EmailFolder emailFolder) that maps EmailFolder to Microsoft Graph folder path
- Support common folder types using Graph API folder paths:
    - Inbox: "Inbox"
    - Sent: "SentItems"
    - Drafts: "Drafts"  
    - Spam: "JunkEmail"
    - Trash: "DeletedItems"
    - Custom: use ServiceSpecificId as folder name/ID
- Update GetEmail method to call GetEmailsFromFolder with Inbox as default folder
- Ensure the refactored code reuses existing connection management and email mapping logic

## 9.6 - Maintain backward compatibility
- All existing GetEmail method signatures must remain unchanged
- All existing functionality must continue to work exactly as before
- Default behavior (when no folder specified) should retrieve from Inbox
- No breaking changes to existing entities or interfaces

## 9.7 - Error handling and logging
- Add appropriate error handling for invalid or non-existent folders
- Log folder resolution and email retrieval operations
- Provide clear error messages when folders cannot be found or accessed
- Handle service-specific folder permission issues

## 9.8 - Testing requirements
- Verify all existing GetEmail tests still pass
- Test folder resolution for each service
- Test error handling for invalid folders
- Verify backward compatibility is maintained

