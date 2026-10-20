using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace WebApp.Keycloak;

// Fetches a client-credentials token from the phaseolus realm and caches it until
// shortly before it expires, per architecture plan Q7 (BeanApp is a confidential
// client with no end-user login flow).
public class KeycloakTokenClient(IHttpClientFactory httpClientFactory, IOptions<KeycloakOptions> options)
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private string? _cachedToken;
    private DateTimeOffset _expiresAt = DateTimeOffset.MinValue;

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        if (IsCachedTokenValid())
        {
            return _cachedToken!;
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (IsCachedTokenValid())
            {
                return _cachedToken!;
            }

            var o = options.Value;
            var client = httpClientFactory.CreateClient("Keycloak");

            using var response = await client.PostAsync(
                $"{o.Authority}/protocol/openid-connect/token",
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "client_credentials",
                    ["client_id"] = o.ClientId,
                    ["client_secret"] = o.ClientSecret,
                }),
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var payload = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken)
                ?? throw new InvalidOperationException("Keycloak token response was empty");

            _cachedToken = payload.AccessToken;
            _expiresAt = DateTimeOffset.UtcNow.AddSeconds(payload.ExpiresIn);

            return _cachedToken;
        }
        finally
        {
            _lock.Release();
        }
    }

    // 10s buffer so a token never expires mid-flight to OrderBeans
    private bool IsCachedTokenValid() =>
        _cachedToken is not null && DateTimeOffset.UtcNow < _expiresAt - TimeSpan.FromSeconds(10);

    private record TokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn);
}
