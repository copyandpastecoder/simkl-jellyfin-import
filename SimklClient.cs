using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SimklJellyfinImport;

public class SimklClient(string clientId, string accessToken)
{
    private const string BaseUrl = "https://api.simkl.com";
    private const int BatchSize = 100;

    private readonly HttpClient _http = new()
    {
        DefaultRequestHeaders =
        {
            { "User-Agent", "simkl-jellyfin-import/1.0" },
            { "Authorization", $"Bearer {accessToken}" }
        }
    };

    private readonly JsonSerializerOptions _jsonOpts = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public async Task ImportMoviesAsync(List<WatchedMovie> movies)
    {
        Console.WriteLine($"\nImporting {movies.Count} movies...");
        int imported = 0;

        foreach (var batch in movies.Chunk(BatchSize))
        {
            var payload = new
            {
                movies = batch.Select(m => new
                {
                    title = m.Title,
                    year = m.Year,
                    watched_at = m.WatchedAt?.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    ids = BuildIds(m.ImdbId, m.TmdbId)
                }).ToArray()
            };

            await PostHistoryAsync(payload);
            imported += batch.Length;
            Console.WriteLine($"  {imported}/{movies.Count} movies submitted");
            await Task.Delay(1100); // stay under 1 req/sec limit
        }

        Console.WriteLine("✓ Movies done.");
    }

    public async Task ImportEpisodesAsync(List<WatchedEpisode> episodes)
    {
        Console.WriteLine($"\nImporting {episodes.Count} episodes across {episodes.Select(e => e.SeriesImdbId ?? e.SeriesTitle).Distinct().Count()} shows...");

        // Group by series
        var bySeries = episodes
            .GroupBy(e => new { e.SeriesTitle, e.SeriesImdbId, e.SeriesTmdbId, e.SeriesYear })
            .ToList();

        int totalShows = bySeries.Count;
        int doneShows = 0;

        foreach (var seriesGroup in bySeries)
        {
            var seasonMap = seriesGroup
                .GroupBy(e => e.Season)
                .Select(sg => new
                {
                    number = sg.Key,
                    episodes = sg.Select(e => new
                    {
                        number = e.Episode,
                        watched_at = e.WatchedAt?.ToString("yyyy-MM-ddTHH:mm:ssZ")
                    }).ToArray()
                }).ToArray();

            var payload = new
            {
                shows = new[]
                {
                    new
                    {
                        title = seriesGroup.Key.SeriesTitle,
                        year = seriesGroup.Key.SeriesYear,
                        ids = BuildIds(seriesGroup.Key.SeriesImdbId, seriesGroup.Key.SeriesTmdbId),
                        seasons = seasonMap
                    }
                }
            };

            await PostHistoryAsync(payload);
            doneShows++;
            Console.WriteLine($"  {doneShows}/{totalShows}: {seriesGroup.Key.SeriesTitle} ({seriesGroup.Count()} episodes)");
            await Task.Delay(1100);
        }

        Console.WriteLine("✓ Shows done.");
    }

    private async Task PostHistoryAsync(object payload)
    {
        var json = JsonSerializer.Serialize(payload, _jsonOpts);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var url = $"{BaseUrl}/sync/history?client_id={clientId}&app-name=simkl-jellyfin-import&app-version=1.0";

        var response = await _http.PostAsync(url, content);
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new Exception($"SIMKL API error {response.StatusCode}: {body}");
    }

    private static object? BuildIds(string? imdb, string? tmdb)
    {
        if (imdb == null && tmdb == null) return null;
        return new { imdb, tmdb };
    }
}
