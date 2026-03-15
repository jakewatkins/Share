# Email services

Email Services will be a web API that allows applications to delete or move emails in either Google Email or Microsoft Outlook. The service will make sue of the existing emailServices library to integration with both Microsoft's and Google's apis.  It will also make use of the KeyVaultTokenCache to access the tokens needed to take action on behalf of the user.

## delete email
the service will take the following parameters to delete an email:
- the email service name (Gmail or Outlook)
- the user's email
- the email id

The service will use the user's email to get the token and authenticate with the email service and will then use the id of the email to delete it.

## move email
the service will take the following parameters to move an email:
- the email service name (Gmail or Outlook)
- the user's email
- the email id
- the destination folder name
The service will use the user's email to get the token and authenticate with the email service, get the email using the id and then move it to the destination folder.

If an error occurs doing either of these operations:
- log the error using the Microsoft.Extentions.Logging framework
- return an HTTP 500 error

