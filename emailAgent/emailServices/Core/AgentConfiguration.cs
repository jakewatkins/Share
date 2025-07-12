using Microsoft.Extensions.Configuration;
using Azure.Security.KeyVault.Secrets;
using Azure.Identity;
using System;

namespace EmailAgent.Core
{
    /// <summary>
    /// Configuration loader that retrieves settings from Azure Key Vault and application configuration.
    /// Handles all Key Vault interactions so services don't need to deal with Key Vault directly.
    /// </summary>
    public class AgentConfiguration
    {
        // Key Vault secrets - loaded during construction
        public string OwaServiceURI { get; private set; } = string.Empty;
        public string OwaPassword { get; private set; } = string.Empty;
        public string OwaEmailAddress { get; private set; } = string.Empty;
        public string GoogleCalendarId { get; private set; } = string.Empty;
        public string GoogleClientId { get; private set; } = string.Empty;
        public string GoogleClientSecret { get; private set; } = string.Empty;
        public string OutlookClientId { get; private set; } = string.Empty;
        public string OutlookSecret { get; private set; } = string.Empty;

        // General configuration - loaded from settings.json
        public string KeyvaultName { get; private set; } = string.Empty;
        public int RetrievalCount { get; private set; }
        public int MaxAttachmentSize { get; private set; }

        /// <summary>
        /// Initializes a new instance of AgentConfiguration.
        /// Loads all Key Vault secrets during construction (synchronously).
        /// </summary>
        /// <param name="configuration">The application configuration interface</param>
        /// <exception cref="ArgumentException">Thrown when keyvaultName is missing from configuration</exception>
        /// <exception cref="InvalidOperationException">Thrown when unable to retrieve secrets from Key Vault</exception>
        public AgentConfiguration(IConfiguration configuration)
        {
            // Load general configuration settings
            LoadGeneralConfiguration(configuration);

            // Load Key Vault secrets
            LoadKeyVaultSecrets();
        }

        /// <summary>
        /// Loads general configuration settings from settings.json with defaults
        /// </summary>
        private void LoadGeneralConfiguration(IConfiguration configuration)
        {
            // KeyvaultName is required - throw exception if missing
            KeyvaultName = configuration["keyvaultName"] 
                ?? throw new ArgumentException("keyvaultName is required in configuration but was not found");

            // RetrievalCount with default value
            if (int.TryParse(configuration["RetrievalCount"], out int retrievalCount) && retrievalCount > 0)
            {
                RetrievalCount = retrievalCount;
            }
            else
            {
                RetrievalCount = 500; // Default value
            }

            // MaxAttachmentSize with default value
            if (int.TryParse(configuration["MaxAttachmentSize"], out int maxAttachmentSize) && maxAttachmentSize > 0)
            {
                MaxAttachmentSize = maxAttachmentSize;
            }
            else
            {
                MaxAttachmentSize = 1048576; // Default value (1MB)
            }
        }

        /// <summary>
        /// Loads all secrets from Azure Key Vault synchronously
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when unable to retrieve secrets from Key Vault</exception>
        private void LoadKeyVaultSecrets()
        {
            try
            {
                // Create Key Vault client using DefaultAzureCredential
                var keyVaultUri = new Uri($"https://{KeyvaultName}.vault.azure.net/");
                var client = new SecretClient(keyVaultUri, new DefaultAzureCredential());

                // Load all secrets synchronously using GetAwaiter().GetResult()
                OwaServiceURI = GetSecretValue(client, "owaServiceURI");
                OwaPassword = GetSecretValue(client, "owaPassword");
                OwaEmailAddress = GetSecretValue(client, "owaEmailAddress");
                GoogleCalendarId = GetSecretValue(client, "googleCalendarId");
                GoogleClientId = GetSecretValue(client, "googleClientId");
                GoogleClientSecret = GetSecretValue(client, "googleClientSecret");
                OutlookClientId = GetSecretValue(client, "outlookClientId");
                OutlookSecret = GetSecretValue(client, "outlookSecret");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Unable to retrieve secrets from Key Vault '{KeyvaultName}'. Error: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Retrieves a secret value from Key Vault synchronously
        /// </summary>
        /// <param name="client">The Key Vault secret client</param>
        /// <param name="secretName">The name of the secret to retrieve</param>
        /// <returns>The secret value</returns>
        /// <exception cref="InvalidOperationException">Thrown when secret cannot be retrieved</exception>
        private string GetSecretValue(SecretClient client, string secretName)
        {
            try
            {
                var response = client.GetSecretAsync(secretName).GetAwaiter().GetResult();
                return response.Value.Value;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve secret '{secretName}' from Key Vault", ex);
            }
        }
    }
}
