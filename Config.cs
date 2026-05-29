namespace SimklJellyfinImport;

public class Config
{
    public string JellyfinDbPath { get; set; } = "";
    public string SimklClientId { get; set; } = "";
    public string SimklAccessToken { get; set; } = "";

    public static Config Load(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Config file not found: {path}\nCopy appsettings.example.json to appsettings.json and fill in your values.");

        var json = File.ReadAllText(path);
        return System.Text.Json.JsonSerializer.Deserialize<Config>(json,
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new Exception("Failed to parse config file.");
    }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(JellyfinDbPath))
            throw new Exception("JellyfinDbPath is required in appsettings.json");
        if (!File.Exists(JellyfinDbPath))
            throw new FileNotFoundException($"Jellyfin database not found: {JellyfinDbPath}");
        if (string.IsNullOrWhiteSpace(SimklClientId))
            throw new Exception("SimklClientId is required in appsettings.json");
    }
}
