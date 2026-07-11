# IncursionTracker

A BepInEx client-side mod for Mycopunk that adds a compact HUD for tracking Incursion mode stats.

## Features

Tracks the following during an Incursion run:

- **Total mission time** – elapsed time since the Incursion objective started
- **Time remaining** – synced mission countdown
- **Floor** – current floor number (includes +5 jumps from door codes)
- **Abominations killed** – boss-room abominations defeated
- **Levers pulled** – time levers activated
- **Door codes input** – successful keypad code entries

### Floor 30 freeze

Optional config toggle (`StopTrackingAtFloor30`, default **on**) freezes all trackers once you reach floor 30, so your “run to 30” stats stay locked while you continue deeper if you want.

### HUD

- Top-left compact multi-line panel
- Only visible during an active Incursion
- Can be fully disabled via config

## Dependencies

* Mycopunk
* [BepInEx](https://github.com/BepInEx/BepInEx) – Version 5.4.2403 or compatible
* .NET Framework 4.8
* HarmonyLib (included via NuGet / BepInEx)

## Building

```bash
dotnet build --configuration Release
```

## Installing

**Via Thunderstore (Recommended)**  
Install with your mod manager.

**Manual**  
Place `IncursionTracker.dll` in `<Mycopunk>/BepInEx/plugins/`.

## Configuration

Config file: `BepInEx/config/sparroh.incursiontracker.cfg`

| Setting | Default | Description |
|---|---|---|
| `EnableIncursionTrackerHUD` | `true` | Show/hide the tracker HUD |
| `StopTrackingAtFloor30` | `true` | Freeze all trackers at floor 30 |

## Notes

- Client-side only; works as host or client.
- Late joiners only track stats from the moment they join.
- Floor display uses the game’s current floor (code skips jump +5).

## Authors

- Sparroh

## License

MIT – see LICENSE
