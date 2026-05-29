using Microsoft.Data.Sqlite;

namespace SimklJellyfinImport;

public class JellyfinReader(string dbPath)
{
    private const string MovieType = "MediaBrowser.Controller.Entities.Movies.Movie";
    private const string EpisodeType = "MediaBrowser.Controller.Entities.TV.Episode";

    public List<WatchedMovie> GetWatchedMovies()
    {
        var movies = new List<WatchedMovie>();

        using var conn = new SqliteConnection($"Data Source={dbPath};Mode=ReadOnly");
        conn.Open();

        var sql = """
            SELECT
                b.Name,
                b.ProductionYear,
                (SELECT ProviderValue FROM BaseItemProviders WHERE ItemId=b.Id AND ProviderId='Imdb' LIMIT 1) AS ImdbId,
                (SELECT ProviderValue FROM BaseItemProviders WHERE ItemId=b.Id AND ProviderId='Tmdb' LIMIT 1) AS TmdbId,
                MAX(ud.LastPlayedDate) AS LastPlayed
            FROM BaseItems b
            JOIN UserData ud ON ud.ItemId = b.Id
            WHERE b.Type = @type AND ud.Played = 1
            GROUP BY b.Id
            ORDER BY b.Name
            """;

        using var cmd = new SqliteCommand(sql, conn);
        cmd.Parameters.AddWithValue("@type", MovieType);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            movies.Add(new WatchedMovie(
                Title: reader.GetString(0),
                Year: reader.IsDBNull(1) ? null : reader.GetInt32(1),
                ImdbId: reader.IsDBNull(2) ? null : reader.GetString(2),
                TmdbId: reader.IsDBNull(3) ? null : reader.GetString(3),
                WatchedAt: reader.IsDBNull(4) ? null : ParseDate(reader.GetString(4))
            ));
        }

        return movies;
    }

    public List<WatchedEpisode> GetWatchedEpisodes()
    {
        var episodes = new List<WatchedEpisode>();

        using var conn = new SqliteConnection($"Data Source={dbPath};Mode=ReadOnly");
        conn.Open();

        var sql = """
            SELECT
                e.SeriesName,
                s.ProductionYear,
                (SELECT ProviderValue FROM BaseItemProviders WHERE ItemId=e.SeriesId AND ProviderId='Imdb' LIMIT 1) AS SeriesImdb,
                (SELECT ProviderValue FROM BaseItemProviders WHERE ItemId=e.SeriesId AND ProviderId='Tmdb' LIMIT 1) AS SeriesTmdb,
                e.ParentIndexNumber AS Season,
                e.IndexNumber AS Episode,
                ud.LastPlayedDate
            FROM BaseItems e
            JOIN UserData ud ON ud.ItemId = e.Id
            LEFT JOIN BaseItems s ON s.Id = e.SeriesId
            WHERE e.Type = @type
              AND ud.Played = 1
              AND e.ParentIndexNumber IS NOT NULL
              AND e.IndexNumber IS NOT NULL
            ORDER BY e.SeriesName, e.ParentIndexNumber, e.IndexNumber
            """;

        using var cmd = new SqliteCommand(sql, conn);
        cmd.Parameters.AddWithValue("@type", EpisodeType);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            episodes.Add(new WatchedEpisode(
                SeriesTitle: reader.GetString(0),
                SeriesYear: reader.IsDBNull(1) ? null : reader.GetInt32(1),
                SeriesImdbId: reader.IsDBNull(2) ? null : reader.GetString(2),
                SeriesTmdbId: reader.IsDBNull(3) ? null : reader.GetString(3),
                Season: reader.GetInt32(4),
                Episode: reader.GetInt32(5),
                WatchedAt: reader.IsDBNull(6) ? null : ParseDate(reader.GetString(6))
            ));
        }

        return episodes;
    }

    private static DateTime? ParseDate(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        if (DateTime.TryParse(raw, out var dt)) return dt.ToUniversalTime();
        return null;
    }
}
