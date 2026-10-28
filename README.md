# Survey Sites and Flags

Survey Sites and Flags is an unofficial Moon Sweepers 1.0.5 mod that adds every survey site and flag to the orange Moon Map inside stations.

## Features

- Shows all known flags and survey sites on the station Moon Map.
- Uses the game's world coordinates, so markers line up with their real locations on the globe.
- Undiscovered locations are gray.
- Untangled flags and surveyed sites turn bright green.
- Avoids duplicate native markers for discovered locations.
- No extra HUD, F9 menu, or changed controls.

## Installation

1. Install BepInEx 6 IL2CPP for Moon Sweepers.
2. Extract the release archive into the folder containing `MoonSweepers.exe`.
3. Confirm the file exists at `BepInEx/plugins/SurveySitesAndFlags/SurveySitesAndFlags.dll`.
4. Start the game, walk to the orange station console, and press `E` for Moon map.

Requirements:

- Moon Sweepers 1.0.5, Windows x64
- BepInEx 6 IL2CPP win-x64: https://github.com/BepInEx/BepInEx/releases
- Moon Sweepers BepInEx patcher/bootstrap used by the game setup

## Uninstall

Remove `BepInEx/plugins/SurveySitesAndFlags`.

## Notes

This mod reads the game's existing landmark list and shared raised/surveyed state. Multiplayer behavior depends on the game's normal world-state synchronization and should be tested with the same mod version on every player.

Moon Sweepers, BepInEx, Il2CppInterop, and the community patcher belong to their respective creators. This is an unofficial community mod.
