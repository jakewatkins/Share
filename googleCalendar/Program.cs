using System;
using System.Text.Json;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Logging;
using Google.Apis.Services;

namespace test
{

    public class GoogleTest
    {
        

        private static Settings LoadSettings()
        {
            var settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
            if (!File.Exists(settingsPath))
            {
                throw new FileNotFoundException("Settings file not found.");
            }

            var settingsJson = File.ReadAllText(settingsPath);
            return JsonSerializer.Deserialize<Settings>(settingsJson);
        }

        public static void Main()
        {
            // See https://aka.ms/new-console-template for more information
            Settings settings = LoadSettings();

            var credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    new ClientSecrets
                    {
                        ClientId = settings.clientId,
                        ClientSecret = settings.clientSecret,
                    },
                    new[] { CalendarService.Scope.Calendar },
                    "user",
                    CancellationToken.None).Result;

            var service = new CalendarService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "Google Calender API v3",
            });

            var queryStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var queryEnd = queryStart.AddDays(2);

            var query = service.Events.List(settings.calendarId);
            // query.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime; - not supported :(
            query.TimeMin = queryStart;
            query.TimeMax = queryEnd;

            var events = query.Execute().Items;

            var eventList = events.ToList();

            Console.WriteLine("<div>Agenda</div>");
            Console.WriteLine("<table>");
            foreach (var item in eventList)
            {
                Console.WriteLine("<tr>");
                //Console.WriteLine("{0}\t{1}", item.Item1, item.Item2);
                Console.WriteLine("<td>{0}</td><td>{1}</td>", item.Summary, item.Start.DateTime, item.End.DateTime);
                Console.WriteLine("</tr>");
            }
            Console.WriteLine("</table>");
        }

    }


    class Settings
    {
        public string clientId { get; set; }
        public string clientSecret { get; set; }
        public string calendarId { get; set; }
    }
}
