using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Services;
using System.Text.RegularExpressions;

public class Settings
{
    public string ClientId { get; }
    public string ClientSecret { get; }
    public string CalendarId { get; }

    public Settings(string keyVaultName)
    {
        var kvUri = $"https://{keyVaultName}.vault.azure.net/";
        var client = new SecretClient(new Uri(kvUri), new DefaultAzureCredential());
        ClientId = client.GetSecret("googleClientId").Value.Value;
        ClientSecret = client.GetSecret("googleClientSecret").Value.Value;
        CalendarId = client.GetSecret("googleCalendarId").Value.Value;
    }
}
