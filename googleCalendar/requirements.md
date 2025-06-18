# Google Cal requirements
- Google Cal is a C# application.
- Agenda uses the google calendar API to retrieve the current day's events and generates a report for me.
- there will only be 1 calendar to access
- for Oauth the needed values are stored in my azure keyvault.   
    - the keyvault name is kvGPSecrets
    - the client id is in the secret googleClientId
    - the client secret is in the secret googleClientSecret
    - the calendar id is in the secret googleCalendarId
    - use the nuget library Azure.Security.KeyVault.Secrets to access the keyvault
- The day starts at 0600cst and ends at 1800cst
    - the only time zone we are worried about is cst
    - all day events should be included.
    - recurring events are handled like any other event that is on the day's agenda
    - events that start before 0600cst can be ignored
    - events that start at 0600cst but extend beyond 1800cst should be included 
    - events that start after 1800cst can be ignored
    - if an event has a blank summary skip it
    - there is not maximum number of events in a day
- the report is output in HTML using a table with two columns
    - the first column shows the start time (no date)
    - the second column shows the title (summary) of the event
    - if possible, if we can find a link to a zoom meeting in the event the second column would be a hyperlink and the address would be the zoom meeting.  Using the domain 'zoom.us' would work for detecting a zoom meeting.  Either description or location will be acceptable.  Only use the first zoom link found regardless of where it is found.
    - the overall report file only needs to have the tabel and its rows and columns, no other html structures are needed.

# Error handling
- use the normal try/catch approach to catch errors where it makes sense
- show the main error message in the console when it occurs and stop the program there.  

# Requirement 1 refactor to production like project
- extract LoadSettings to it's own class, call the class settings
    - get the 3 settings from the keyvault using the constructor
    - expose the 3 settings as public properties
- extract GoogleTest into it's own file and rename to CalendarAgent
- put the code to get the calendar information in a method named "GetEvents"
- write the events information as HTML in to the output file, append the information to the end of the file.
- in program.cs create a Main class with a main method that sets everything up
    - create an instance of settings to pass to CalendarAgent
    - parse the command line for the name of the output file.
        - there will only be 1 command line parameter
        - the filename must be specified. if it is missing show an error message and stop
        - if the file specified does not exist then create it.
    - create an instance of CalendarAgent passing it the settings and the name of the report file


