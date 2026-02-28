# TODO

## batch mode
email service needs to be updated so that it saves the token and renews it so I don't have to login each time the agent runs.

## emailProcessor
After emails are classified we need to process them.  There are several issues I need to handle:
- is this a known sender?
    - for instance the bank, amazon, utilities, etc. are all known senders who should be handled
    - many of these will just need a process to handle
- category
    - there will be a configuration file that contains information for how to process each category
        - category
        - URL
    - Workflows will be created in N8N that will process the emails
        - the processor will post the entire email to the workflow
        - the same workflow can be used by multiple categories
        - need a way to delete or move emails
            - need both
            - spam stuff gets deleted
            - things that might be useful should be moved to an archive
