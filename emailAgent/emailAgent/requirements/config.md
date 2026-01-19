# Email Agent Configuration

## overview
I'm getting ready to create a new application that will use emailServices to retrieve email from multiple mail boxes.  A few of the mailboxes will use the same service but have different email addresses or user names associated with them.  
I want to store the configuration in a json file:
    - there will be a section for google hosted email boxes
        - array of email addresses like:
            [
                "jake.watkins@gmail.com", "jake.watkins@commonspirit.com"
            ]
    - there will be a section for outlook hosted email boxes
        - array of email addresses and passwords
            [
                {"email":"runsincirclesscreaming@msn.com", "pwd":"password"}
            ]
    - there will be a section for Outlook Web Access email boxes
        - array of server and email addresses and passwords
            [
                {"server":"mail.domain.com", "email":"jakew@guerillaprogrammer.com", "pwd": "password"}
            ]

In order for this to work we will need to update emailServices.

## emailServices update
- make the properties (like OwaServiceURI) has public setters.
    that way a hosting program can modify the values as needed.
