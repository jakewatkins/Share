# Email Classifier

EmailClassifier is a python script that uses Apple's MLX framework to load an LLM and then classify emails in a json file.
This is the process the script will use:
    - load configuration from settings.json
    - Load the model specified by the setting "Classifier" from HuggingFace
        - we will be using a model that was fine tuned for this purpose.
        - Ideally we will load the model one time and just keep sending it emails until we've processed all of them
    - Get a list of email files in the EmailTemp directory setting
         - the json files in the directory will contain the email in json format
    - create a new file called "classified-emails-{datetime}.json
        - datetime will be a date & time string formatted as yyyyMMddhhmmss
        - the ProcessedEmails will be the directory where the classified emails file will be stored.
    - for each file:
        - open the email file
        - for each email
            - put a blue dot on the screen to show progress
            - use the Prompt setting and the contents of the email to get a classification for the email from the LLM
                - put the email's body in the {email_body} place holder
                - the list of categories in the prompt is final and what was used for training the model
            - add a "classification" field to the email object and save the classification returned by the LLM
            - save the classified email to the classified emails file
                - always append to the file
        - after all of the emails in the file have been classified put a green '!' on the screen
        - change the file's extension to '.bak' so we don't process it again
    - after finishing all of files print "done" in green on a new line

## Error handling
    - in general if an error occurs print a helpful diagnostic message and exit
    - if the LLM is unavailable - print a message indicating that the model is not available and exit
    - if the email file is corrupt or contains invalid json: print a message indicating this problem and exit
    - if the LLM gives us an invalid classification print a red '#' on the screen and store "other" as the classification and continue

## Logging
    - create a log file named "emailClassifier-{datetime}.log" in the same directory as the script
        - datetime will be formatted as yyyyMMddHHmmss
    - log the following events with timestamps:
        - script start time and configuration loaded
        - model loading start and completion
        - start and end of processing each email file
        - any errors or warnings that occur
        - script completion with summary statistics (total emails processed, classifications made, errors encountered)
    - log entries should include timestamp in format: yyyy-MM-dd HH:mm:ss
    - for unattended operation, log file should be the primary way to track script execution and troubleshoot issues

## Clarifications
- we'll only use the email body for now.
- no other progress indicators will be needed.  Eventually this will be running unattended.
- if an email body is empty: skip it
- html content handling: the model was trained with the html as is, so we'll just pass it along.
- directory creation - they will already exist.  if they are missing print an error and exit
- file processing order doesnt matter.
- the admin will make sure the EmailTemp directory only contains email files.
- there will be a new classified emails file created each time the script runs.  we only want 1 for each run.
- no limits on the MLX config. Use it all!  That's why we're buying a BIC MAC for this stuff.

# configuration
{
    "Classifier": "jake-watkins/email-classifier",
    "EmailTemp" : "/Users/jakewatkins/temp/email",
    "ProcessedEmails" : "/Users/jakewatkins/temp/processedEmails",
    "Prompt" : "Classify the following email into a single-word category. Respond with ONLY one word representing the category from this list: [promotional,transactional,notification,security,event,educational,newsletter,survey,business,personal,solicitation,recruitment,membership,political,informative,account,press,memorial,admission]. Email content: {email_body}"

}
# example email
{
    "id": "19b1937508743970",
    "service": "Gmail",
    "from": "HWXLR8 \u003Cnotifications@github.com\u003E",
    "to": [
      "\u0022ceoloide/ergogen-footprints\u0022 \u003Cergogen-footprints@noreply.github.com\u003E"
    ],
    "cc": [
      "Subscribed \u003Csubscribed@noreply.github.com\u003E"
    ],
    "bcc": [],
    "sentDateTime": "2025-12-13T13:36:53-06:00",
    "subject": "[ceoloide/ergogen-footprints] Change default side of filled zone to F\u0026B to match comment (PR #75)",
    "body": "\u003Cp dir=\u0022auto\u0022\u003EComment for filled zone says:\u003C/p\u003E\r\n\u003Cpre class=\u0022notranslate\u0022\u003E\u003Ccode class=\u0022notranslate\u0022\u003E// Params:\r\n//    side: default is \u0027F\u0026amp;B\u0027 for both Front and Back\r\n\u003C/code\u003E\u003C/pre\u003E\r\n\u003Cp dir=\u0022auto\u0022\u003Ebut the code sets the side to front:\u003C/p\u003E\r\n\u003Cpre class=\u0022notranslate\u0022\u003E\u003Ccode class=\u0022notranslate\u0022\u003E  params: {\r\n    side: \u0027F\u0027,\r\n\u003C/code\u003E\u003C/pre\u003E\r\n\u003Cp dir=\u0022auto\u0022\u003Eunfortunately we need to escape the ampersand or else the final output to kicad will result in \u003Ccode class=\u0022notranslate\u0022\u003ENaN.Cu\u003C/code\u003E after evaluating \u003Ccode class=\u0022notranslate\u0022\u003EF\u0026amp;B\u003C/code\u003E.\u003C/p\u003E\r\n\r\n\u003Chr\u003E\r\n\r\n\u003Ch4\u003EYou can view, comment on, or merge this pull request online at:\u003C/h4\u003E\r\n\u003Cp\u003E\u0026nbsp;\u0026nbsp;\u003Ca href=\u0027https://github.com/ceoloide/ergogen-footprints/pull/75\u0027\u003Ehttps://github.com/ceoloide/ergogen-footprints/pull/75\u003C/a\u003E\u003C/p\u003E\r\n\r\n\u003Ch4\u003ECommit Summary\u003C/h4\u003E\r\n\u003Cul\u003E\r\n  \u003Cli\u003E\u003Ca href=\u0022https://github.com/ceoloide/ergogen-footprints/pull/75/commits/f0d8408a141f4c855a6c8f5df908324c7d58ba79\u0022 class=\u0022commit-link\u0022\u003Ef0d8408\u003C/a\u003E  change default side to F\u0026amp;B\u003C/li\u003E\r\n\u003C/ul\u003E\r\n\r\n\u003Ch4 style=\u0022display: inline-block\u0022\u003EFile Changes \u003C/h4\u003E \u003Cp style=\u0022display: inline-block\u0022\u003E(\u003Ca href=\u0022https://github.com/ceoloide/ergogen-footprints/pull/75/files\u0022\u003E1\u0026nbsp;file\u003C/a\u003E)\u003C/p\u003E\r\n\u003Cul\u003E\r\n  \u003Cli\u003E\r\n    \u003Cstrong\u003EM\u003C/strong\u003E\r\n    \u003Ca href=\u0022https://github.com/ceoloide/ergogen-footprints/pull/75/files#diff-24d66c47e1491cc63d2c4bfe5e583179f38881242a1e58692a439db59bb66ab3\u0022\u003Eutility_filled_zone.js\u003C/a\u003E\r\n    (9)\r\n  \u003C/li\u003E\r\n\u003C/ul\u003E\r\n\r\n\u003Ch4\u003EPatch Links:\u003C/h4\u003E\r\n\u003Cul\u003E\r\n  \u003Cli\u003E\u003Ca href=\u0027https://github.com/ceoloide/ergogen-footprints/pull/75.patch\u0027\u003Ehttps://github.com/ceoloide/ergogen-footprints/pull/75.patch\u003C/a\u003E\u003C/li\u003E\r\n  \u003Cli\u003E\u003Ca href=\u0027https://github.com/ceoloide/ergogen-footprints/pull/75.diff\u0027\u003Ehttps://github.com/ceoloide/ergogen-footprints/pull/75.diff\u003C/a\u003E\u003C/li\u003E\r\n\u003C/ul\u003E\r\n\r\n\u003Cp style=\u0022font-size:small;-webkit-text-size-adjust:none;color:#666;\u0022\u003E\u0026mdash;\u003Cbr /\u003EReply to this email directly, \u003Ca href=\u0022https://github.com/ceoloide/ergogen-footprints/pull/75\u0022\u003Eview it on GitHub\u003C/a\u003E, or \u003Ca href=\u0022https://github.com/notifications/unsubscribe-auth/ADBBFWXXLLVC5MTAMY3D6B34BRTFLAVCNFSM6AAAAACO6OIKN2VHI2DSMVQWIX3LMV43ASLTON2WKOZTG4ZDMMZRGIZTSOA\u0022\u003Eunsubscribe\u003C/a\u003E.\u003Cbr /\u003EYou are receiving this because you are subscribed to this thread.\u003Cimg src=\u0022https://github.com/notifications/beacon/ADBBFWT5Y75XWQZQCJFMBT34BRTFLA5CNFSM6AAAAACO6OIKN2WGG33NNVSW45C7OR4XAZNFJFZXG5LFVJRW63LNMVXHIX3JMTHN4GYDZY.gif\u0022 height=\u00221\u0022 width=\u00221\u0022 alt=\u0022\u0022 /\u003E\u003Cspan style=\u0022color: transparent; font-size: 0; display: none; visibility: hidden; overflow: hidden; opacity: 0; width: 0; height: 0; max-width: 0; max-height: 0; mso-hide: all\u0022\u003EMessage ID: \u003Cspan\u003E\u0026lt;ceoloide/ergogen-footprints/pull/75\u003C/span\u003E\u003Cspan\u003E@\u003C/span\u003E\u003Cspan\u003Egithub\u003C/span\u003E\u003Cspan\u003E.\u003C/span\u003E\u003Cspan\u003Ecom\u0026gt;\u003C/span\u003E\u003C/span\u003E\u003C/p\u003E\r\n\u003Cscript type=\u0022application/ld\u002Bjson\u0022\u003E[\r\n{\r\n\u0022@context\u0022: \u0022http://schema.org\u0022,\r\n\u0022@type\u0022: \u0022EmailMessage\u0022,\r\n\u0022potentialAction\u0022: {\r\n\u0022@type\u0022: \u0022ViewAction\u0022,\r\n\u0022target\u0022: \u0022https://github.com/ceoloide/ergogen-footprints/pull/75\u0022,\r\n\u0022url\u0022: \u0022https://github.com/ceoloide/ergogen-footprints/pull/75\u0022,\r\n\u0022name\u0022: \u0022View Pull Request\u0022\r\n},\r\n\u0022description\u0022: \u0022View this Pull Request on GitHub\u0022,\r\n\u0022publisher\u0022: {\r\n\u0022@type\u0022: \u0022Organization\u0022,\r\n\u0022name\u0022: \u0022GitHub\u0022,\r\n\u0022url\u0022: \u0022https://github.com\u0022\r\n}\r\n}\r\n]\u003C/script\u003E\r\n",
    "attachments": []
  }

  ## Email Classification Prompt
Classify the following email into a single-word category.
Respond with ONLY one word representing the category from this list:
promotional,transactional,notification,security,event,educational,newsletter,survey,business,personal,solicitation,recruitment,membership,political,informative,account,press,memorial,admission

Email content:
{email_body}
