using SimklJellyfinImport;

Console.WriteLine("=== simkl-jellyfin-import ===\n");

var configPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
// fallback to working directory
if (!File.Exists(configPath))
    configPath = "appsettings.json";

Config config;
try
{
    config = Config.Load(configPath);
    config.Validate();
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Config error: {ex.Message}");
    return 1;
}

// PIN auth if no token yet
if (string.IsNullOrWhiteSpace(config.SimklAccessToken))
{
    var auth = new SimklAuth(config.SimklClientId);
    var token = await auth.GetAccessTokenAsync();

    Console.WriteLine($"\nSave this access token in appsettings.json > SimklAccessToken:");
    Console.WriteLine(token);

    config.SimklAccessToken = token;
}

var db = new JellyfinReader(config.JellyfinDbPath);
var client = new SimklClient(config.SimklClientId, config.SimklAccessToken);

// Movies
Console.Write("Reading watched movies from Jellyfin... ");
var movies = db.GetWatchedMovies();
Console.WriteLine($"{movies.Count} found.");

// Episodes
Console.Write("Reading watched episodes from Jellyfin... ");
var episodes = db.GetWatchedEpisodes();
Console.WriteLine($"{episodes.Count} found.");

if (movies.Count == 0 && episodes.Count == 0)
{
    Console.WriteLine("Nothing to import.");
    return 0;
}

Console.Write("\nReady to import to SIMKL. Continue? [Y/n] ");
var answer = Console.ReadLine()?.Trim().ToLower();
if (answer == "n") return 0;

await client.ImportMoviesAsync(movies);
await client.ImportEpisodesAsync(episodes);

Console.WriteLine("\n✓ Import complete!");
return 0;
