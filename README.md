# ZKO (Zeek Keyboard Overlay)

An [Everest](https://everestapi.github.io/) mod for Celeste that draws a keyboard overlay in-game. Each key lights up while its action is held, and it follows whatever you have bound in Celeste's own keyboard/controller config.

```
[   DASH    ] [ ↑ ] [GRB]
[ CD ] [ ← ] [ ↓ ] [ → ]
[ESC ] [      JUMP      ]
```

Direction keys show the key you actually bound (W/A/S/D, arrows, ...).

## Install

Drop `ZKO.zip` into `Celeste/Mods` (or build from source, which installs it for you). Needs Everest 5935 or newer.

## Settings (Mod Options → ZKO)

| Setting | What it does |
|---|---|
| Show Overlay | Master on/off |
| Show Overlay In | Gameplay only / Gameplay + pause menu / Everywhere (map and menus too) |
| Position | Top Left, Bottom Left (default), Top Right, Bottom Right, Custom |
| Edge Padding | Distance from the screen edge for the corner presets (default 2% / 5%) |
| Custom Position X / Y | Used when Position is Custom (0% = left/top, 100% = right/bottom) |
| Scale, Opacity | Size and transparency |
| Key / Outline / Pressed / Label Colour | Preset list plus exact R/G/B sliders (hold left/right to move faster) |
| Invert Key Colour / Outline / Labels | Draws that part by inverting whatever is behind it, so it is always visible. Pressed keys keep the pressed colour. |
| Debug: Show Lag Spikes | When a frame takes over 40 ms, shows where the time went (game update, game render, ZKO, garbage collection, other) and writes it to `log.txt` |
| Toggle Overlay | Optional hotkey, set under Keyboard Config / Controller Config |

Colours are also stored as `RRGGBB` hex in `Celeste/Saves/modsettings-ZKO.celeste`.

## Build

Needs the .NET 8 SDK and Celeste with Everest.

```
dotnet build Source/ZKO.csproj -p:CelestePrefix="C:\path\to\Celeste"
```

`CelestePrefix` defaults to the standard Steam folder. Each build copies the mod into `<CelestePrefix>\Mods\ZKO`. A `-c Release` build also writes `ZKO.zip`.

## How it works

- `Source/OverlayRenderer.cs` draws the overlay after the frame is finished (hook on `Engine.RenderCore`), straight onto the screen. That is what lets the invert options use a real inverting blend.
- Keys are drawn procedurally as 1px rows (slanted, rounded, anti-aliased), so no art assets are needed.
- Dash, Crouch Dash, Grab, Jump and Pause read Celeste's own virtual buttons. Directions read the raw bindings so opposite directions can light up together.
