

using System;
using Microsoft.Exchange.WebServices.Data;

namespace EmailAgent
{
    public class OwaAgent
    {
        public void GetEmail(Settings settings)
        {
            // Initialize the Exchange Service
            ExchangeService service = new ExchangeService(ExchangeVersion.Exchange2013_SP1);
            service.Credentials = new WebCredentials(settings.EmailAddress, settings.Password);

            // Set the URL of the Exchange Server
            service.Url = new Uri(settings.ServerURI);

            try
            {
                // Define the folder to search (Inbox in this case)
                FolderId inboxFolder = WellKnownFolderName.Inbox;

                // Define the search filter (optional)
                ItemView view = new ItemView(10); // Fetch the top 10 emails
                view.Offset = 0;
                view.OrderBy.Add(ItemSchema.DateTimeReceived, SortDirection.Descending);
                view.PropertySet = new PropertySet(BasePropertySet.IdOnly, ItemSchema.Subject, ItemSchema.DateTimeReceived);

                // Retrieve emails
                FindItemsResults<Item> findResults = service.FindItems(WellKnownFolderName.Inbox, view);

                Console.WriteLine("Emails retrieved:");
                foreach (Item item in findResults.Items)
                {
                    Console.WriteLine($"Subject: {item.Subject}");
                    Console.WriteLine($"Received: {item.DateTimeReceived}");
                    Console.WriteLine("-----------------------------");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}