namespace SimklJellyfinImport;

public record WatchedMovie(
    string Title,
    int? Year,
    string? ImdbId,
    string? TmdbId,
    DateTime? WatchedAt
);

public record WatchedEpisode(
    string SeriesTitle,
    int? SeriesYear,
    string? SeriesImdbId,
    string? SeriesTmdbId,
    int Season,
    int Episode,
    DateTime? WatchedAt
);
