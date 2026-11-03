# WGA Raid Hub Sync

A small Windows tray app that carries data between World of Warcraft and the
[WGA Raid Hub](https://github.com/katogaming88/WGA-Raid-Hub) site. The game
can't reach the internet, so Sync does that part:

- **Into the game:** writes the team's loot priority data as a small data
  addon, which the in-game addon reads at login. This replaces pasting an
  import string with `/rcpl import`.
- **Out of the game:** after you log out, uploads what the in-game addon
  saved (vault, crests, bonus rolls) and RCLootCouncil's loot history.

It works alongside two other pieces:

| Piece | What it is |
|---|---|
| [WGA Raid Hub](https://github.com/katogaming88/WGA-Raid-Hub) | The website and its database |
| [WGA Raid Hub Addon](https://github.com/katogaming88/RCLootCouncil_PriorityLoot) | The in-game addon every raider installs |
| WGA Raid Hub Sync (this repo) | Run by team officers, at least one per team |

Raiders only install the addon. Any loot officer on a team can run Sync. One
officer is enough, because the addon shares the team's weekly data with
guildmates.

## Status

Early. Sync runs in the tray, finds your World of Warcraft folder (or lets
you choose it), and lists each game account with its characters so you can
tick which ones to sync. Linking to a Raid Hub account waits on the site's
link page, which comes after the new site goes live.

## Building

Needs Windows and the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

```
dotnet build
dotnet test
dotnet run --project src/Sync
```

Sync keeps what you picked in `%LOCALAPPDATA%\WGA Raid Hub Sync\settings.json`.

See [CONTRIBUTING.md](CONTRIBUTING.md) for branches, pull requests and
releases.
