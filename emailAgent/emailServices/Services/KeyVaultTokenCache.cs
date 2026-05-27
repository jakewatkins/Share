using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using System.Text;
using System.Text.Json;

namespace EmailAgent.Services
{
    /// <summary>
    /// MSAL token cache implementation that stores tokens in Azure Key Vault
    /// Replaces file-based token storage for Outlook authentication
    /// </summary>
    public class KeyVaultTokenCache
    {
        private readonly KeyVaultService _keyVaultService;
        private readonly string _emailAddress;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the KeyVaultTokenCache
        /// </summary>
        /// <param name="keyVaultService">Service for interacting with Azure Key Vault</param>
        /// <param name="emailAddress">Email address associated with this token cache</param>
        /// <param name="logger">Logger for diagnostic information</param>
        public KeyVaultTokenCache(KeyVaultService keyVaultService, string emailAddress, ILogger logger)
        {
            _keyVaultService = keyVaultService ?? throw new ArgumentNullException(nameof(keyVaultService));
            _emailAddress = emailAddress ?? throw new ArgumentNullException(nameof(emailAddress));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Called before MSAL accesses the cache.
        /// Loads token data from Azure Key Vault into MSAL's in-memory cache.
        /// Must return Task (not void) so MSAL can await completion via SetBeforeAccessAsync.
        /// </summary>
        /// <param name="args">Token cache notification arguments</param>
        public async Task BeforeAccessNotification(TokenCacheNotificationArgs args)
        {
            try
            {
                _logger.LogDebug("Loading Outlook tokens from Key Vault for: {EmailAddress}", _emailAddress);

                // Retrieve token data from Key Vault
                var tokenJson = await _keyVaultService.GetOutlookTokenAsync(_emailAddress);

                if (!string.IsNullOrEmpty(tokenJson))
                {
                    // Convert JSON string to bytes for MSAL
                    var tokenBytes = Encoding.UTF8.GetBytes(tokenJson);

                    // Deserialize the token cache data into MSAL's cache
                    args.TokenCache.DeserializeMsalV3(tokenBytes);

                    _logger.LogDebug("Successfully loaded Outlook token cache data from Key Vault for: {EmailAddress}", _emailAddress);
                }
                else
                {
                    _logger.LogDebug("No existing Outlook token found in Key Vault for: {EmailAddress}", _emailAddress);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load Outlook tokens from Key Vault for: {EmailAddress}", _emailAddress);
                // Don't throw - allow MSAL to continue with empty cache
            }
        }

        /// <summary>
        /// Called after MSAL accesses the cache.
        /// Saves token data from MSAL's in-memory cache to Azure Key Vault.
        /// Must return Task (not void) so MSAL can await completion via SetAfterAccessAsync.
        /// </summary>
        /// <param name="args">Token cache notification arguments</param>
        public async Task AfterAccessNotification(TokenCacheNotificationArgs args)
        {
            try
            {
                // Only save if the cache has changed
                if (args.HasStateChanged)
                {
                    _logger.LogDebug("Saving Outlook tokens to Key Vault for: {EmailAddress}", _emailAddress);

                    // Serialize MSAL cache data to bytes
                    var tokenBytes = args.TokenCache.SerializeMsalV3();

                    if (tokenBytes != null && tokenBytes.Length > 0)
                    {
                        // Convert bytes to JSON string for Key Vault storage
                        var tokenJson = Encoding.UTF8.GetString(tokenBytes);

                        // Store in Key Vault
                        await _keyVaultService.SetOutlookTokenAsync(_emailAddress, tokenJson);

                        _logger.LogDebug("Successfully saved Outlook token cache data to Key Vault for: {EmailAddress}", _emailAddress);
                    }
                    else
                    {
                        _logger.LogDebug("No token data to save for: {EmailAddress}", _emailAddress);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save Outlook tokens to Key Vault for: {EmailAddress}", _emailAddress);
                // Don't throw - allow MSAL to continue functioning
            }
        }
    }
}
