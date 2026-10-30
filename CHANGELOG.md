# Changelog

## 0.1.4

- Changed discovered flags and surveyed sites to high-contrast red markers so they remain visible after the moon is completely swept.
- Kept undiscovered landmark markers muted for quick status recognition.

## 0.1.3

- Fixed fresh-save maps showing no flags or survey sites until a landmark was discovered.
- Initializes the complete landmark catalog before rendering the map.
- Added native-shaped fallback markers when the game's discovered-marker pools are empty.
- Uses the live Moon Map view dimensions for marker projection, keeping icons aligned with the orange globe across resolutions and UI scales.
- Prevents duplicate native markers while retaining gray undiscovered and green completed states.

## 0.1.0

- Initial public release.
- Added all flags and survey sites to the station Moon Map.
