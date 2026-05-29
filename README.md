# simkl-jellyfin-import

A .NET 8 CLI tool that reads your **Jellyfin** watch history and imports it into **SIMKL** with per-episode accuracy — not just "last episode watched".

## Why

SIMKL's CSV import only supports a `LastEpWatched` field per show, which assumes you watched every episode in order. If you skipped any episodes, the CSV will incorrectly mark them as watched.

This tool uses the SIMKL API to import **exactly** the episodes you actually watched, with the correct watched date for each one.

## What it imports

- ✅ All watched movies — with watched date
- ✅ All watched TV episodes — per-episode, per-season, with watched dates
- ✅ IMDB IDs used for reliable matching
- ✅ Safe to run multiple times — SIMKL deduplicates already-watched items

## Requirements

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8) or later
- A [SIMKL](https://simkl.com) account
- Access to your `jellyfin.db` SQLite database file

---

## Setup

### Step 1 — Get your Jellyfin database

You need a local copy of `jellyfin.db`. The Jellyfin DB is opened **read-only** — nothing on your server is modified.

**Docker (most common):**

If Jellyfin runs in Docker, copy the DB out of the container. For example using Docker CLI:
```bash
docker cp jellyfin:/config/data/data/jellyfin.db ./jellyfin.db
```
Or copy it to a network share and reference it by UNC path.

**Default paths by platform:**
| Platform | Path |
|---|---|
| Docker | `/config/data/data/jellyfin.db` (inside container) |
| Linux (native) | `/var/lib/jellyfin/data/jellyfin.db` |
| Windows (native) | `%APPDATA%\Jellyfin\data\jellyfin.db` |
| macOS (native) | `~/.local/share/jellyfin/data/jellyfin.db` |

---

### Step 2 — Create a SIMKL app

You need your own SIMKL API app — it's free and takes 30 seconds.

1. Go to [simkl.com/settings/developer](https://simkl.com/settings/developer/)
2. Click **Create New App**
3. Fill in any name (e.g. `jellyfin-import`)
4. Set **Redirect URI** to exactly: `urn:ietf:wg:oauth:2.0:oob`
5. Click Save
6. Copy your **Client ID** from the app page

---

### Step 3 — Configure

Clone this repo and create your config file:

```bash
git clone https://github.com/copyandpastecoder/simkl-jellyfin-import.git
cd simkl-jellyfin-import
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

> **Leave `SimklAccessToken` empty** — on first run the tool will walk you through a PIN-based login and display your token. Paste it back into `appsettings.json` so you don't need to log in again.

> **Windows UNC paths** use double backslashes: `"\\\\SERVER\\Share\\jellyfin.db"`

---

### Step 4 — Run

```bash
dotnet run
```

**First run** (no access token):
1. The tool prints a URL and a short PIN code
2. Open the URL in your browser: `https://simkl.com/pin`
3. Enter the PIN and approve
4. The tool detects the approval and continues automatically
5. Copy the printed access token into `appsettings.json` > `SimklAccessToken`

**Subsequent runs** (token saved):
```
=== simkl-jellyfin-import ===

Reading watched movies from Jellyfin... 132 found.
Reading watched episodes from Jellyfin... 2092 found.

Ready to import to SIMKL. Continue? [Y/n]
```

The tool will show progress as it imports:
```
Importing 132 movies...
  100/132 movies submitted
  132/132 movies submitted
✓ Movies done.

Importing 2092 episodes across 23 shows...
  1/23: Breaking Bad (62 episodes)
  2/23: House (114 episodes)
  ...
✓ Shows done.

✓ Import complete!
```

---

### Step 5 — Build a standalone binary (optional)

If you want to run without the .NET SDK installed:

```bash
# Windows
dotnet publish -c Release -r win-x64 --self-contained -o ./publish/win

# Linux
dotnet publish -c Release -r linux-x64 --self-contained -o ./publish/linux

# macOS (Intel)
dotnet publish -c Release -r osx-x64 --self-contained -o ./publish/mac

# macOS (Apple Silicon)
dotnet publish -c Release -r osx-arm64 --self-contained -o ./publish/mac-arm
```

Copy `appsettings.json` to the same folder as the binary before running.

---

## How it works

The tool reads Jellyfin's SQLite database directly (read-only) and queries:
- `BaseItems` — movie and episode metadata (title, year, season, episode number)
- `UserData` — watched status and last played date per item
- `BaseItemProviders` — external IDs (IMDB, TMDB) for reliable SIMKL matching

It then calls `POST /sync/history` on the SIMKL API:
- **Movies** are batched 100 per request
- **TV shows** are grouped by series — all seasons and episodes for a show go in a single request

This means a typical library uses **~25 API requests**, well within SIMKL's 1,000/day limit.

---

## Notes

- Episodes without a season or episode number in Jellyfin are skipped
- The SIMKL API enforces 1 request/second — large libraries may take a few minutes
- If you have multiple Jellyfin users, the tool imports the union of all watched history
- `appsettings.json` is gitignored — your credentials will never be committed
