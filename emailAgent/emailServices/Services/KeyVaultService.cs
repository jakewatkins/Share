using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace EmailAgent.Services
{
    /// <summary>
    /// Service for managing OAuth tokens and other secrets in Azure Key Vault
    /// </summary>
    public class KeyVaultService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<KeyVaultService> _logger;
        private readonly SecretClient _secretClient;
        private readonly string _keyVaultName;

        /// <summary>
        /// Initializes a new instance of the KeyVaultService
        /// </summary>
        /// <param name="configuration">Application configuration for reading Key Vault settings</param>
        /// <param name="logger">Logger for diagnostic information</param>
        /// <exception cref="ArgumentNullException">Thrown when configuration or logger is null</exception>
        /// <exception cref="ArgumentException">Thrown when keyvaultName is missing from configuration</exception>
        public KeyVaultService(IConfiguration configuration, ILogger<KeyVaultService> logger)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Get Key Vault name from configuration
            _keyVaultName = _configuration["keyvaultName"]
                ?? throw new ArgumentException("keyvaultName is required in configuration but was not found");

            _logger.LogInformation("Initializing KeyVault service for vault: {KeyVaultName}", _keyVaultName);

            try
            {
                // Create Key Vault client using DefaultAzureCredential for managed identity support
                var keyVaultUri = new Uri($"https://{_keyVaultName}.vault.azure.net/");
                _secretClient = new SecretClient(keyVaultUri, new DefaultAzureCredential());

                _logger.LogInformation("KeyVault service initialized successfully for vault: {KeyVaultName}", _keyVaultName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize KeyVault service for vault: {KeyVaultName}", _keyVaultName);
                throw new InvalidOperationException($"Failed to initialize KeyVault service for vault '{_keyVaultName}'", ex);
            }
        }

        /// <summary>
        /// Stores or updates a secret in Key Vault
        /// </summary>
        /// <param name="secretName">Name of the secret</param>
        /// <param name="secretValue">Value of the secret</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the async operation</returns>
        /// <exception cref="ArgumentException">Thrown when secretName or secretValue is invalid</exception>
        /// <exception cref="InvalidOperationException">Thrown when Key Vault operation fails</exception>
        public async Task SetSecretAsync(string secretName, string secretValue, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(secretName))
                throw new ArgumentException("Secret name cannot be null or empty", nameof(secretName));

            if (string.IsNullOrWhiteSpace(secretValue))
                throw new ArgumentException("Secret value cannot be null or empty", nameof(secretValue));

            try
            {
                _logger.LogDebug("Setting secret: {SecretName} in Key Vault: {KeyVaultName}", secretName, _keyVaultName);

                var secret = new KeyVaultSecret(secretName, secretValue);
                var response = await _secretClient.SetSecretAsync(secret, cancellationToken);

                _logger.LogInformation("Successfully stored/updated secret: {SecretName} in Key Vault: {KeyVaultName}", secretName, _keyVaultName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set secret: {SecretName} in Key Vault: {KeyVaultName}", secretName, _keyVaultName);
                throw new InvalidOperationException($"Failed to set secret '{secretName}' in Key Vault '{_keyVaultName}'", ex);
            }
        }

        /// <summary>
        /// Retrieves a secret from Key Vault
        /// </summary>
        /// <param name="secretName">Name of the secret to retrieve</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The secret value, or null if not found</returns>
        /// <exception cref="ArgumentException">Thrown when secretName is invalid</exception>
        /// <exception cref="InvalidOperationException">Thrown when Key Vault operation fails</exception>
        public async Task<string?> GetSecretAsync(string secretName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(secretName))
                throw new ArgumentException("Secret name cannot be null or empty", nameof(secretName));

            try
            {
                _logger.LogDebug("Retrieving secret: {SecretName} from Key Vault: {KeyVaultName}", secretName, _keyVaultName);

                var response = await _secretClient.GetSecretAsync(secretName, cancellationToken: cancellationToken);
                var secretValue = response.Value.Value;

                _logger.LogDebug("Successfully retrieved secret: {SecretName} from Key Vault: {KeyVaultName}", secretName, _keyVaultName);
                return secretValue;
            }
            catch (Azure.RequestFailedException ex) when (ex.Status == 404)
            {
                _logger.LogDebug("Secret not found: {SecretName} in Key Vault: {KeyVaultName}", secretName, _keyVaultName);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get secret: {SecretName} from Key Vault: {KeyVaultName}", secretName, _keyVaultName);
                throw new InvalidOperationException($"Failed to get secret '{secretName}' from Key Vault '{_keyVaultName}'", ex);
            }
        }

        /// <summary>
        /// Deletes a secret from Key Vault
        /// </summary>
        /// <param name="secretName">Name of the secret to delete</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the async operation</returns>
        /// <exception cref="ArgumentException">Thrown when secretName is invalid</exception>
        /// <exception cref="InvalidOperationException">Thrown when Key Vault operation fails</exception>
        public async Task DeleteSecretAsync(string secretName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(secretName))
                throw new ArgumentException("Secret name cannot be null or empty", nameof(secretName));

            try
            {
                _logger.LogDebug("Deleting secret: {SecretName} from Key Vault: {KeyVaultName}", secretName, _keyVaultName);

                await _secretClient.StartDeleteSecretAsync(secretName, cancellationToken);

                _logger.LogInformation("Successfully deleted secret: {SecretName} from Key Vault: {KeyVaultName}", secretName, _keyVaultName);
            }
            catch (Azure.RequestFailedException ex) when (ex.Status == 404)
            {
                _logger.LogDebug("Secret not found for deletion: {SecretName} in Key Vault: {KeyVaultName}", secretName, _keyVaultName);
                // Not throwing an error if secret doesn't exist - this is idempotent
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete secret: {SecretName} from Key Vault: {KeyVaultName}", secretName, _keyVaultName);
                throw new InvalidOperationException($"Failed to delete secret '{secretName}' from Key Vault '{_keyVaultName}'", ex);
            }
        }

        /// <summary>
        /// Stores a Gmail OAuth token in Key Vault using the standard naming convention
        /// </summary>
        /// <param name="emailAddress">The Gmail email address</param>
        /// <param name="tokenJson">The OAuth token as JSON string</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the async operation</returns>
        public async Task SetGmailTokenAsync(string emailAddress, string tokenJson, CancellationToken cancellationToken = default)
        {
            var secretName = GetGmailTokenSecretName(emailAddress);
            await SetSecretAsync(secretName, tokenJson, cancellationToken);
        }

        /// <summary>
        /// Retrieves a Gmail OAuth token from Key Vault using the standard naming convention
        /// </summary>
        /// <param name="emailAddress">The Gmail email address</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The OAuth token as JSON string, or null if not found</returns>
        public async Task<string?> GetGmailTokenAsync(string emailAddress, CancellationToken cancellationToken = default)
        {
            var secretName = GetGmailTokenSecretName(emailAddress);
            return await GetSecretAsync(secretName, cancellationToken);
        }

        public async Task DeleteGmailTokenAsync(string emailAddress, CancellationToken cancellationToken = default)
        {
            var secretName = GetGmailTokenSecretName(emailAddress);
            await DeleteSecretAsync(secretName, cancellationToken);
        }

        /// <summary>
        /// Stores an Outlook OAuth token in Key Vault using the standard naming convention
        /// </summary>
        /// <param name="emailAddress">The Outlook email address</param>
        /// <param name="tokenJson">The OAuth token as JSON string</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the async operation</returns>
        public async Task SetOutlookTokenAsync(string emailAddress, string tokenJson, CancellationToken cancellationToken = default)
        {
            var secretName = GetOutlookTokenSecretName(emailAddress);
            await SetSecretAsync(secretName, tokenJson, cancellationToken);
        }

        /// <summary>
        /// Retrieves an Outlook OAuth token from Key Vault using the standard naming convention
        /// </summary>
        /// <param name="emailAddress">The Outlook email address</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The OAuth token as JSON string, or null if not found</returns>
        public async Task<string?> GetOutlookTokenAsync(string emailAddress, CancellationToken cancellationToken = default)
        {
            var secretName = GetOutlookTokenSecretName(emailAddress);
            return await GetSecretAsync(secretName, cancellationToken);
        }

        /// <summary>
        /// Generates the secret name for a Gmail token using the standard naming convention
        /// </summary>
        /// <param name="emailAddress">The Gmail email address</param>
        /// <returns>The standardized secret name</returns>
        private static string GetGmailTokenSecretName(string emailAddress)
        {
            var sanitizedEmail = SanitizeEmailAddress(emailAddress);
            return $"gmail-token-{sanitizedEmail}";
        }

        /// <summary>
        /// Generates the secret name for an Outlook token using the standard naming convention
        /// </summary>
        /// <param name="emailAddress">The Outlook email address</param>
        /// <returns>The standardized secret name</returns>
        private static string GetOutlookTokenSecretName(string emailAddress)
        {
            var sanitizedEmail = SanitizeEmailAddress(emailAddress);
            return $"outlook-token-{sanitizedEmail}";
        }

        /// <summary>
        /// Sanitizes an email address for use in Key Vault secret names
        /// </summary>
        /// <param name="emailAddress">The email address to sanitize</param>
        /// <returns>The sanitized email address</returns>
        private static string SanitizeEmailAddress(string emailAddress)
        {
            if (string.IsNullOrWhiteSpace(emailAddress))
                throw new ArgumentException("Email address cannot be null or empty", nameof(emailAddress));

            // Replace '@' and '.' with hyphens for Key Vault secret name compatibility
            return emailAddress.Replace("@", "-").Replace(".", "-").ToLowerInvariant();
        }
    }
}
