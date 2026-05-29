using System.Text.Json;

namespace SimklJellyfinImport;

public class SimklAuth(string clientId)
{
    private const string BaseUrl = "https://api.simkl.com";
    private readonly HttpClient _http = new() { DefaultRequestHeaders = { { "User-Agent", "simkl-jellyfin-import/1.0" } } };

    public async Task<string> GetAccessTokenAsync()
    {
        // Request PIN
        var pinResp = await _http.GetStringAsync($"{BaseUrl}/oauth/pin?client_id={clientId}&redirect=urn:ietf:wg:oauth:2.0:oob");
        var pin = JsonDocument.Parse(pinResp).RootElement;

        var userCode = pin.GetProperty("user_code").GetString()!;
        var deviceCode = pin.GetProperty("device_code").GetString()!;
        var url = pin.GetProperty("verification_url").GetString()!;
        var interval = pin.GetProperty("interval").GetInt32();
        var expiresIn = pin.GetProperty("expires_in").GetInt32();

        Console.WriteLine($"\nOpen: {url}");
        Console.WriteLine($"Enter code: {userCode}");
        Console.WriteLine("Waiting for approval...\n");

        var deadline = DateTime.UtcNow.AddSeconds(expiresIn);
        while (DateTime.UtcNow < deadline)
        {
            await Task.Delay(TimeSpan.FromSeconds(interval));
            try
            {
                var pollResp = await _http.GetStringAsync($"{BaseUrl}/oauth/pin/{deviceCode}?client_id={clientId}");
                var result = JsonDocument.Parse(pollResp).RootElement;
                if (result.TryGetProperty("access_token", out var token))
                {
                    Console.WriteLine("✓ Authorized!");
                    return token.GetString()!;
                }
            }
            catch { /* still pending */ }
        }

        throw new Exception("PIN expired. Please run again.");
    }
}
