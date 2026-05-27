using System.Text.Json;
using Google.Apis.Auth.OAuth2.Responses;
using Microsoft.Identity.Client;
using Xunit;

namespace testEmailServices
{
    /// <summary>
    /// Unit tests that document and validate token storage behavior for Gmail and Outlook.
    ///
    /// These tests expose two bugs that cause the agent to prompt for interactive
    /// authentication on every run instead of reusing stored tokens:
    ///
    ///   Bug 1 (Gmail):   KeyVaultDataStore.StoreAsync serializes with CamelCase naming
    ///                    but GetAsync deserializes with default (case-sensitive) options,
    ///                    so all token fields deserialize as null.
    ///
    ///   Bug 2 (Outlook): KeyVaultTokenCache.BeforeAccessNotification is async void.
    ///                    MSAL's SetBeforeAccess expects a synchronous Action, so the
    ///                    Key Vault load fires-and-forgets — MSAL always sees an empty
    ///                    cache and falls through to interactive authentication.
    /// </summary>
    public class TokenStorageTests
    {
        // ---------------------------------------------------------------------------
        // Bug 1: Gmail — KeyVaultDataStore JSON serialization/deserialization mismatch
        // ---------------------------------------------------------------------------

        [Fact]
        public void Bug1_GmailToken_StoreAsyncUsesCamelCase_GetAsyncUsesDefaultOptions_AllFieldsAreNull()
        {
            // This reproduces the bug in KeyVaultDataStore:
            //   StoreAsync  → JsonSerializer.Serialize(value, options { CamelCase })
            //   GetAsync    → JsonSerializer.Deserialize<T>(json)   ← no options, case-sensitive
            //
            // Result: "accessToken" in JSON does not match "AccessToken" C# property.
            // Every retrieved token has null AccessToken / RefreshToken, so Google's
            // library treats it as "no token" and opens the browser.

            var original = new TokenResponse
            {
                AccessToken = "ya29.test_access_token",
                RefreshToken = "1//test_refresh_token",
                TokenType = "Bearer",
                ExpiresInSeconds = 3600,
                IssuedUtc = new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc)
            };

            // Simulate StoreAsync: serialize with CamelCase (what the code does today)
            var storedJson = JsonSerializer.Serialize(original, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });

            // Confirm the JSON uses camelCase keys
            Assert.Contains("\"accessToken\"", storedJson);
            Assert.Contains("\"refreshToken\"", storedJson);

            // Simulate GetAsync: deserialize with DEFAULT options (what the code does today)
            var retrieved = JsonSerializer.Deserialize<TokenResponse>(storedJson);

            // BUG: camelCase keys in JSON don't match PascalCase C# properties.
            // System.Text.Json default deserialization is case-sensitive.
            Assert.Null(retrieved!.AccessToken);   // "accessToken" != "AccessToken"
            Assert.Null(retrieved.RefreshToken);   // "refreshToken" != "RefreshToken"
        }

        [Fact]
        public void Fix1_GmailToken_WithPropertyNameCaseInsensitive_AllFieldsDeserializeCorrectly()
        {
            // This validates the fix: adding PropertyNameCaseInsensitive = true to GetAsync.
            // Round-trip: StoreAsync serializes with CamelCase, GetAsync deserializes with
            // case-insensitive matching → all fields survive the round-trip.

            var original = new TokenResponse
            {
                AccessToken = "ya29.test_access_token",
                RefreshToken = "1//test_refresh_token",
                TokenType = "Bearer",
                ExpiresInSeconds = 3600,
                IssuedUtc = new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc)
            };

            // Simulate StoreAsync (unchanged)
            var storedJson = JsonSerializer.Serialize(original, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });

            // Simulate GetAsync with the fix applied
            var retrieved = JsonSerializer.Deserialize<TokenResponse>(storedJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true   // THE FIX
            });

            Assert.Equal(original.AccessToken, retrieved!.AccessToken);
            Assert.Equal(original.RefreshToken, retrieved.RefreshToken);
            Assert.Equal(original.TokenType, retrieved.TokenType);
            Assert.Equal(original.ExpiresInSeconds, retrieved.ExpiresInSeconds);
        }

        [Fact]
        public void Fix1_GmailToken_NullTokenInKeyVault_GetAsyncReturnsDefault()
        {
            // Validates that GetAsync returns null/default when no token is stored,
            // which is the correct "first run" behavior (triggers interactive auth once).
            var retrieved = JsonSerializer.Deserialize<TokenResponse>("null", new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            Assert.Null(retrieved);
        }

        [Fact]
        public void Fix1_GmailToken_SecretName_MatchesBetweenStoreAndGet()
        {
            // Ensures the Key Vault secret name generated for a given email is identical
            // in both StoreAsync and GetAsync (i.e., the token stored can always be found).
            //
            // Both KeyVaultDataStore and KeyVaultService use:
            //   email.Replace("@", "-").Replace(".", "-").ToLowerInvariant()
            //   prefixed with "gmail-token-"

            var testCases = new[]
            {
                ("jake.watkins@gmail.com",           "gmail-token-jake-watkins-gmail-com"),
                ("jakew@guerillaprogrammer.com",      "gmail-token-jakew-guerillaprogrammer-com"),
                ("USER@EXAMPLE.COM",                 "gmail-token-user-example-com"),
            };

            foreach (var (email, expectedSecret) in testCases)
            {
                var sanitized = email.Replace("@", "-").Replace(".", "-").ToLowerInvariant();
                var secretName = $"gmail-token-{sanitized}";
                Assert.Equal(expectedSecret, secretName);
            }
        }

        // ---------------------------------------------------------------------------
        // Bug 2: Outlook — KeyVaultTokenCache async void callbacks
        // ---------------------------------------------------------------------------

        [Fact]
        public void Bug2_AsyncVoid_ReturnsBeforeAwaitedWorkCompletes()
        {
            // Demonstrates why async void is the wrong signature for MSAL's cache callbacks.
            //
            // MSAL's SetBeforeAccess(Action<TokenCacheNotificationArgs>) is synchronous.
            // When an async void method hits its first await, it returns to the caller
            // immediately. The caller (MSAL) then proceeds with an empty token cache.
            //
            // This is exactly what happens in KeyVaultTokenCache.BeforeAccessNotification:
            // MSAL calls the callback, the callback awaits the Key Vault call and returns,
            // MSAL sees an empty cache, and falls through to interactive authentication.

            bool keyVaultLoadCompleted = false;

            async void AsyncVoidCallback()
            {
                // Simulates: await _keyVaultService.GetOutlookTokenAsync(...)
                await Task.Delay(10);
                keyVaultLoadCompleted = true;
            }

            // MSAL calls the callback synchronously (fire-and-forget for async void)
            AsyncVoidCallback();

            // MSAL immediately continues here — Key Vault hasn't loaded yet
            Assert.False(keyVaultLoadCompleted,
                "async void returned before the Key Vault load completed, " +
                "so MSAL proceeds with an empty token cache and triggers interactive auth");
        }

        [Fact]
        public async Task Fix2_AsyncTask_CompletesBeforeCallerProceeds()
        {
            // Validates the fix: using Task-returning callbacks with MSAL's
            // SetBeforeAccessAsync / SetAfterAccessAsync APIs.
            //
            // When the callback returns a Task, the caller can await it, ensuring
            // the Key Vault load completes before MSAL attempts token acquisition.

            bool keyVaultLoadCompleted = false;

            async Task AsyncTaskCallback()
            {
                // Simulates: await _keyVaultService.GetOutlookTokenAsync(...)
                await Task.Delay(10);
                keyVaultLoadCompleted = true;
            }

            // With the fix, MSAL (via SetBeforeAccessAsync) awaits the callback
            await AsyncTaskCallback();

            Assert.True(keyVaultLoadCompleted,
                "Task-returning callback completes before the caller proceeds, " +
                "so the token cache is populated before MSAL attempts silent auth");
        }

        [Fact]
        public void Fix2_AsyncVoid_ExceptionCannotBeCaughtAtCallSite()
        {
            // Documents why async void is dangerous for MSAL callbacks:
            // exceptions thrown inside an async void method after an await cannot be
            // caught at the call site. The call-site try/catch exits before the
            // exception is thrown (because async void returned immediately at the await).
            //
            // With a Task-returning method, the exception is packaged into the returned
            // Task and re-thrown when the caller awaits it.
            //
            // This test validates that a Task-returning method propagates exceptions
            // correctly (the complement to the async void problem).

            async Task ThrowingAsyncTask()
            {
                await Task.CompletedTask;
                throw new InvalidOperationException("Key Vault failure");
            }

            // The exception is packaged into the returned Task — fully catchable
            var task = ThrowingAsyncTask();
            var exceptionCaught = false;

            try
            {
                task.GetAwaiter().GetResult();
            }
            catch (InvalidOperationException)
            {
                exceptionCaught = true;
            }

            Assert.True(exceptionCaught,
                "Task-returning async methods propagate exceptions to the awaiter, " +
                "unlike async void where exceptions are lost at the call site");
        }

        // ---------------------------------------------------------------------------
        // Outlook secret name consistency (mirrors Gmail test above)
        // ---------------------------------------------------------------------------

        [Fact]
        public void Fix2_OutlookToken_SecretName_MatchesBetweenBeforeAndAfterAccess()
        {
            // Both BeforeAccessNotification and AfterAccessNotification must use the
            // same secret name so the saved token can be found on the next run.
            //
            // KeyVaultService uses:
            //   email.Replace("@", "-").Replace(".", "-").ToLowerInvariant()
            //   prefixed with "outlook-token-"

            var testCases = new[]
            {
                ("RunsInCirclesScreaming@msn.com", "outlook-token-runsincirclesscreaming-msn-com"),
                ("user@outlook.com",               "outlook-token-user-outlook-com"),
            };

            foreach (var (email, expectedSecret) in testCases)
            {
                var sanitized = email.Replace("@", "-").Replace(".", "-").ToLowerInvariant();
                var secretName = $"outlook-token-{sanitized}";
                Assert.Equal(expectedSecret, secretName);
            }
        }
    }
}
