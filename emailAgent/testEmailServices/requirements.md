# Test Email Services
This is a simple program that demonstrates the use of the library emailServices.  It will use OwaService, GmailService, and OutlookService to retrieve emails.

# Requirement 1 - setup
- add a reference to emailServices and add any neede NuGet packages to the project
- add settings.json
- in Program.cs do the following
    - Setup the configuration by loading settings.json and environment variables
- create a class called TestEmailServices in a separate file
    - the constructor will take an instance of IConfiguration and store it in a private member variable for later use
    - there will be a single method called Run that will do the following - 
        - print progress messages as it works
        - creates instances of the email service objects (OwaService, GmailService, OutlookService)
        - for each service:
            - call the GetEmail method
                - for each email returned print the following on a single line:
                    - SentDateTime
                    - Email Service that the email comes from
                    - From 
                    - subject
                separate each field with a dash
            - just call GetEmail once on each service, we just want to see that it works

