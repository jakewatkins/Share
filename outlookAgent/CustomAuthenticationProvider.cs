using Microsoft.Graph;
using Microsoft.Identity.Client;
using System.Net.Http;
using System.Net.Http.Headers;

namespace emailAgent
{
    public class MsalAuthenticationProvider : IAuthenticationProvider
    {
        private readonly IPublicClientApplication _clientApplication;
        private readonly string[] _scopes;

        public MsalAuthenticationProvider(IPublicClientApplication clientApplication, string[] scopes)
        {
            _clientApplication = clientApplication;
            _scopes = scopes;
        }

        public async Task AuthenticateRequestAsync(HttpRequestMessage request)
        {
            var accessToken = await GetAccessTokenAsync();
            if (!string.IsNullOrEmpty(accessToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            }
        }

        private async Task<string?> GetAccessTokenAsync()
        {
            try
            {
                // Try to get token silently first
                var accounts = await _clientApplication.GetAccountsAsync();
                if (accounts.Any())
                {
                    var result = await _clientApplication.AcquireTokenSilent(_scopes, accounts.FirstOrDefault())
                        .ExecuteAsync();
                    return result.AccessToken;
                }

                // Interactive authentication required
                var interactiveResult = await _clientApplication.AcquireTokenInteractive(_scopes)
                    .ExecuteAsync();
                return interactiveResult.AccessToken;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
