using Google.Apis.Util.Store;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace EmailAgent.Services
{
    /// <summary>
    /// Implementation of IDataStore that stores OAuth tokens in Azure Key Vault
    /// Used by Google APIs for persistent token storage
    /// </summary>
    public class KeyVaultDataStore : IDataStore
    {
        private readonly KeyVaultService _keyVaultService;
        private readonly string _emailAddress;
        private readonly ILogger _logger;

        public KeyVaultDataStore(KeyVaultService keyVaultService, string emailAddress, ILogger logger)
        {
            _keyVaultService = keyVaultService ?? throw new ArgumentNullException(nameof(keyVaultService));
            _emailAddress = emailAddress ?? throw new ArgumentNullException(nameof(emailAddress));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Clears all stored data for this email address
        /// </summary>
        /// <returns>Task representing the async operation</returns>
        public async Task ClearAsync()
        {
            try
            {
                _logger.LogDebug("Clearing Gmail tokens from Key Vault for: {EmailAddress}", _emailAddress);

                // Delete the Gmail token from Key Vault
                var secretName = GetGmailTokenSecretName(_emailAddress);
                await _keyVaultService.DeleteSecretAsync(secretName);

                _logger.LogInformation("Cleared Gmail tokens from Key Vault for: {EmailAddress}", _emailAddress);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to clear Gmail tokens from Key Vault for: {EmailAddress}", _emailAddress);
                throw;
            }
        }

        /// <summary>
        /// Deletes a specific key from storage
        /// </summary>
        /// <typeparam name="T">The type of the stored value</typeparam>
        /// <param name="key">The key to delete</param>
        /// <returns>Task representing the async operation</returns>
        public async Task DeleteAsync<T>(string key)
        {
            try
            {
                _logger.LogDebug("Deleting Gmail token from Key Vault for key: {Key}", key);

                var secretName = GetGmailTokenSecretName(key);
                await _keyVaultService.DeleteSecretAsync(secretName);

                _logger.LogDebug("Deleted Gmail token from Key Vault for key: {Key}", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete Gmail token from Key Vault for key: {Key}", key);
                throw;
            }
        }

        /// <summary>
        /// Retrieves a value from storage
        /// </summary>
        /// <typeparam name="T">The type of the value to retrieve</typeparam>
        /// <param name="key">The key to look up</param>
        /// <returns>The stored value or default value if not found</returns>
        public async Task<T> GetAsync<T>(string key)
        {
            try
            {
                _logger.LogDebug("Retrieving Gmail token from Key Vault for key: {Key}", key);

                var secretName = GetGmailTokenSecretName(key);
                var tokenJson = await _keyVaultService.GetSecretAsync(secretName);

                if (string.IsNullOrEmpty(tokenJson))
                {
                    _logger.LogDebug("No Gmail token found in Key Vault for key: {Key}", key);
                    return default(T)!;
                }

                // Deserialize the JSON token.
                // PropertyNameCaseInsensitive is required because StoreAsync serializes
                // with CamelCase (e.g. "accessToken") while C# properties are PascalCase.
                var token = JsonSerializer.Deserialize<T>(tokenJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                _logger.LogDebug("Successfully retrieved Gmail token from Key Vault for key: {Key}", key);
                return token!;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve Gmail token from Key Vault for key: {Key}", key);
                throw;
            }
        }

        /// <summary>
        /// Stores a value in Key Vault
        /// </summary>
        /// <typeparam name="T">The type of the value to store</typeparam>
        /// <param name="key">The key under which to store the value</param>
        /// <param name="value">The value to store</param>
        /// <returns>Task representing the async operation</returns>
        public async Task StoreAsync<T>(string key, T value)
        {
            try
            {
                _logger.LogDebug("Storing Gmail token in Key Vault for key: {Key}", key);

                if (value == null)
                {
                    await DeleteAsync<T>(key);
                    return;
                }

                // Serialize the token to JSON
                var tokenJson = JsonSerializer.Serialize(value, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = false
                });

                var secretName = GetGmailTokenSecretName(key);
                await _keyVaultService.SetSecretAsync(secretName, tokenJson);

                _logger.LogDebug("Successfully stored Gmail token in Key Vault for key: {Key}", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to store Gmail token in Key Vault for key: {Key}", key);
                throw;
            }
        }

        /// <summary>
        /// Generates the Key Vault secret name for a Gmail token
        /// </summary>
        /// <param name="key">The key (typically email address)</param>
        /// <returns>The standardized secret name for Key Vault</returns>
        private static string GetGmailTokenSecretName(string key)
        {
            // Sanitize the key for use in Key Vault secret names
            var sanitizedKey = key.Replace("@", "-").Replace(".", "-").ToLowerInvariant();
            return $"gmail-token-{sanitizedKey}";
        }
    }
}
