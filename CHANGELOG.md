# Changelog

## 1.0.2

- added ui reload after exiting to menu

## 1.0.1
- Mission total time now pauses when the in-game remaining timer is stopped (elevators, inter-floor waits, timer expiry)
- Nudged tracker HUD slightly toward center so it sits fully on-screen
- Fixed door codes being double-counted on host (SendTo.Everyone RPC Send+Execute)
- Fixed tracker HUD not appearing due to patch init failure from RPC stage reflection

## 1.0.0
- Initial release
- Incursion HUD tracking: mission time, remaining time, floor, abominations, levers, door codes
- Optional freeze-at-floor-30 config toggle
- Top-left compact multi-line HUD
