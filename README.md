# simkl-jellyfin-import

A .NET 8 CLI tool that reads your **Jellyfin** watch history and imports it into **SIMKL** with per-episode accuracy — not just "last episode watched".

## Why

SIMKL's CSV import only supports a `LastEpWatched` field per show, which assumes you watched every episode in order. This tool uses the SIMKL API to mark **exactly** the episodes you actually watched, with the correct watched date for each.

## What it imports

- ✅ All watched movies (with watched date)
- ✅ All watched TV episodes (per-episode, per-season, with watched dates)

## Setup

### 1. Get your Jellyfin database

Copy `jellyfin.db` from your Jellyfin server to an accessible path.

- Default location inside Docker: `/config/data/data/jellyfin.db`
- On Windows: `%APPDATA%\Jellyfin\data\jellyfin.db`

### 2. Create a SIMKL app

1. Go to [simkl.com/settings/developer](https://simkl.com/settings/developer/)
2. Create a new app — set Redirect URI to `urn:ietf:wg:oauth:2.0:oob`
3. Copy your **Client ID**

### 3. Configure

```bash
cp appsettings.example.json appsettings.json
```

Edit `appsettings.json`:

```json
{
  "JellyfinDbPath": "/path/to/jellyfin.db",
  "SimklClientId": "your_client_id_here",
  "SimklAccessToken": ""
}
```

Leave `SimklAccessToken` empty — the tool will do a PIN auth on first run and print your token. Paste it back into `appsettings.json` for future runs.

### 4. Run

```bash
dotnet run
```

Or build a self-contained binary:

```bash
dotnet publish -c Release -r win-x64 --self-contained
dotnet publish -c Release -r linux-x64 --self-contained
dotnet publish -c Release -r osx-x64 --self-contained
```

## Notes

- The Jellyfin DB is opened **read-only** — nothing is modified on your server
- SIMKL API rate limit is 1 request/second — large libraries may take a few minutes
- Episodes with no season/episode number in Jellyfin are skipped
- Safe to run multiple times — SIMKL deduplicates already-watched items
