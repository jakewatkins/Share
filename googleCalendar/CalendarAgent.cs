using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using System.Text.RegularExpressions;

public class CalendarAgent
{
    private readonly CalendarService _service;
    private readonly string _calendarId;
    private readonly string _outputFile;

    public CalendarAgent(Settings settings, string outputFile)
    {
        var credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
            new ClientSecrets
            {
                ClientId = settings.ClientId,
                ClientSecret = settings.ClientSecret,
            },
            new[] { CalendarService.Scope.CalendarReadonly },
            "user",
            CancellationToken.None).Result;

        _service = new CalendarService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "Google Calendar API v3",
        });
        _calendarId = settings.CalendarId;
        _outputFile = outputFile;
    }

    public void GetEvents()
    {
        var cstZone = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");
        var today = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, cstZone).Date;
        var start = today.AddHours(6); // 6:00 CST
        var end = today.AddHours(18); // 18:00 CST

        var request = _service.Events.List(_calendarId);
        request.TimeMinDateTimeOffset = start;
        request.TimeMaxDateTimeOffset = end;
        request.SingleEvents = true;
        request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

        var events = request.Execute().Items;
        if (events == null || events.Count == 0) return;

        using (var sw = new StreamWriter(_outputFile, append: true))
        {
            sw.WriteLine("<table>");
            foreach (var ev in events)
            {
                var startTime = ev.Start.DateTimeDateTimeOffset.HasValue ? ev.Start.DateTimeDateTimeOffset.Value.ToString("HH:mm") : "";
                var summary = ev.Summary ?? "";
                var zoom = ExtractZoomLink(ev);
                if (!string.IsNullOrEmpty(zoom))
                    summary = $"<a href=\"{zoom}\">{summary}</a>";
                sw.WriteLine($"<tr><td>{startTime}</td><td>{summary}</td></tr>");
            }
            sw.WriteLine("</table>");
        }
    }

    private string? ExtractZoomLink(Event ev)
    {
        var fields = new[] { ev.Description, ev.Location };
        foreach (var field in fields)
        {
            if (string.IsNullOrEmpty(field)) continue;
            var match = Regex.Match(field, @"https://[\w\.-]*zoom\.us/[\w\?&=\-/%#\.]+", RegexOptions.IgnoreCase);
            if (match.Success) return match.Value;
        }
        return null;
    }
}
