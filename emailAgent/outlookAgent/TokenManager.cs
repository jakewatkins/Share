using Microsoft.Identity.Client;
using Newtonsoft.Json;

namespace emailAgent
{
    public class TokenManager
    {
        private readonly string _tokenFilePath;
        private readonly IPublicClientApplication _app;

        public TokenManager(IPublicClientApplication app, string tokenFilePath = "usertoken.json")
        {
            _app = app;
            _tokenFilePath = tokenFilePath;
        }

        public async Task<AuthenticationResult?> GetTokenAsync(string[] scopes)
        {
            try
            {
                // Try to get token silently first
                var accounts = await _app.GetAccountsAsync();
                if (accounts.Any())
                {
                    var result = await _app.AcquireTokenSilent(scopes, accounts.FirstOrDefault())
                        .ExecuteAsync();
                    await SaveTokenAsync(result);
                    return result;
                }

                // Try to load token from file
                var tokenData = await LoadTokenFromFileAsync();
                if (tokenData != null)
                {
                    // The token cache should handle this automatically
                    var cachedAccounts = await _app.GetAccountsAsync();
                    if (cachedAccounts.Any())
                    {
                        var result = await _app.AcquireTokenSilent(scopes, cachedAccounts.FirstOrDefault())
                            .ExecuteAsync();
                        await SaveTokenAsync(result);
                        return result;
                    }
                }

                // Interactive authentication required
                var interactiveResult = await _app.AcquireTokenInteractive(scopes)
                    .ExecuteAsync();
                await SaveTokenAsync(interactiveResult);
                return interactiveResult;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private async Task SaveTokenAsync(AuthenticationResult result)
        {
            try
            {
                var tokenData = new
                {
                    AccessToken = result.AccessToken,
                    ExpiresOn = result.ExpiresOn,
                    Account = result.Account?.Username,
                    Scopes = result.Scopes
                };

                var json = JsonConvert.SerializeObject(tokenData, Formatting.Indented);
                await File.WriteAllTextAsync(_tokenFilePath, json);
            }
            catch
            {
                // Ignore save errors
            }
        }

        private async Task<dynamic?> LoadTokenFromFileAsync()
        {
            try
            {
                if (!File.Exists(_tokenFilePath))
                    return null;

                var json = await File.ReadAllTextAsync(_tokenFilePath);
                return JsonConvert.DeserializeObject(json);
            }
            catch
            {
                return null;
            }
        }
    }
}
