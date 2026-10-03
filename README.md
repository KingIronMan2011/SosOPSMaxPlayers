# SOS Ops Max Players Mod

A [MelonLoader](https://melonwiki.xyz/) mod for **SOS OPS!** that increases the multiplayer lobby player limit (default: **8**, fully configurable) and automatically expands the game's UI and netcode to support extra players.

> [!WARNING]
> **Disclaimer:** This mod was developed with the assistance of AI (Google Gemini).

---

## Features

- **Configurable Player Limit:** Host lobbies with 6, 8, 12, 16+ players (default is `8`).
- **Steam & EOS Matchmaking Patches:** Overrides `SteamMatchmaking.CreateLobbyAsync` and EOS lobby limits so Steam/EOS create lobbies with extra capacity.
- **Server Full Approval Override:** Hooks `HostingState.GetConnectingStatus` so the host accepts incoming connections beyond 4 players.
- **Auto-Expanding UI:**
  - **Main Menu Pre-Game Lobby (`UILobbyMenuTab`):** Dynamically clones player cards so each player has a visual card, nickname, and kick button.
  - **In-Game Pause/Escape Menu (`UIPlayerList`):** Dynamically duplicates player list rows for all active players.
  - **In-Game Tab Scoreboard (`UILeaderboardOverlay`):** Dynamically expands leaderboard rows.
  - **Lobby Browser:** Displays accurate `Current / Max` count (e.g. `1 / 8`).
- **Index Out of Bounds / Crash Prevention:** Wraps text chat color indexing (`TextChatSessionModule.GetColorByIndex`) to prevent crashes when 5+ players talk in chat.

---

## Installation

1. Download and run the **[MelonLoader Installer](https://github.com/LavaGang/MelonLoader.Installer/releases/latest/download/MelonLoader.Installer.exe)**:
   - In the installer, select `SOS OPS.exe` (v0.6.x or v0.7.x for .NET 6 / IL2CPP).
   - Launch the game once so MelonLoader generates the necessary IL2CPP interop assemblies, then close the game.
2. Download or compile `SOSOpsMaxPlayersMod.dll`.
3. Drop `SOSOpsMaxPlayersMod.dll` into the `Mods/` folder inside your *SOS OPS* game directory:
   ```
   C:\Program Files (x86)\Steam\steamapps\common\SOS OPS\Mods\SOSOpsMaxPlayersMod.dll
   ```
4. Launch the game!

---

## Configuration

After launching the game once with the mod installed, a config file will be created at:
```
SOS OPS/UserData/MelonPreferences.cfg
```

Look for the `[SOSOpsMaxPlayers]` section:
```toml
[SOSOpsMaxPlayers]
MaxPlayers = 8
```
Change `8` to your desired player count and restart the game.

> **Note:** The host must have the mod installed to create the larger lobby. All joining friends should ideally also have the mod installed to avoid client-side UI/networking desync.

---

## Building from Source

Requirements:
- [.NET 6.0+ SDK](https://dotnet.microsoft.com/download)
- MelonLoader installed in *SOS OPS* (to generate `Il2CppAssemblies`)

Build command:
```bash
dotnet build -c Release
```
*(If your SOS OPS is installed in a non-standard path, pass `-p:GameDir="<Path to SOS OPS>"`).*

---

## Disclaimer

This mod was developed with the assistance of AI (Google Gemini).

