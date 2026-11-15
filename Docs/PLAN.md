# ZKO (Zeek Keyboard Overlay): Implementation Plan

Status: **approved 2026-11-15**, decisions in §8. Implementation in progress.
Sources: `Docs/User Input.txt`, `Docs/design.png`.

---

## 1. What we're building

An Everest code mod for Celeste that draws an always-visible keyboard overlay in-game. Each key lights up while its in-game action is held. The keys follow whatever the player has bound in Celeste's own keybinding menu, so there is nothing to configure for it to work.

### Keys (from the doc)
| Overlay key | Celeste action | Label shown |
|---|---|---|
| DASH | Dash | `DASH` |
| CD | Crouch Dash (Everest/1.4 action) | `CD` |
| GRB | Grab | `GRB` |
| JUMP | Jump | `JUMP` |
| Up / Left / Down / Right | Move directions | the **bound key's name** (e.g. `W A S D`, or arrow glyphs if bound to arrows) |
| ESC | Pause / menu | `ESC` |

### Layout (from design.png)
A 4-column, 3-row grid of slanted (italic, parallelogram) keys with rounded corners, white outline, black fill. The pressed colour in the mock-up is olive / yellow-green (`#A8B03A`-ish).

```
Row 1:  [   DASH (2u)   ] [ W ] [GRB]
Row 2:  [ CD ] [ A ] [ S ] [ D ]
Row 3:  [ ?? ] [       JUMP (3u)      ]
```
The bottom-left slot is blank in the design. **Plan: put ESC there** (see open question 1).

---

## 2. Settings (in Mod Options → ZKO)

| Setting | Type | Default |
|---|---|---|
| Show overlay | On/Off toggle | On |
| Toggle overlay hotkey | Key binding (Everest `ButtonBinding`) | unbound |
| Position preset | Top Left / Bottom Left / Top Right / Bottom Right / Custom | Bottom Left |
| Absolute X / Y | Sliders in % of screen (0 to 100), used when preset = Custom | 2% / 95% |
| Edge padding | Slider, % of screen | 5% vertical (per doc: "padded up 5%") |
| Scale | Slider 50% to 200% | 100% |
| Key colour | Colour picker (preset list + hex) | Black fill / white outline |
| Pressed colour | Colour picker (preset list + hex) | Olive `#A8B03A` |
| Opacity | Slider | 100% |
| Show during pause menu | On/Off | On |

"Show / hide" in the doc is covered by the toggle plus an optional hotkey. Everest's settings menu has no built-in colour picker, so colours will be a cycle through named presets (White, Black, Olive, Red, Cyan, Pink, ...) plus a hex value editable in `ZKOModuleSettings` / the save file for anyone wanting an exact colour.

---

## 3. Architecture

Standard Everest code mod, C#, built with the official `CelesteMod` dotnet template (targets .NET 7 / Everest core).

```
ZKO/
  Docs/                       (existing: spec + design)
  ZKO.sln
  everest.yaml                mod id "ZKO", deps: EverestCore
  Source/
    ZKO.csproj
    ZKOModule.cs              EverestModule: Load/Unload hooks, settings type
    ZKOModuleSettings.cs      all settings above, with menu attributes
    Overlay/
      KeyOverlay.cs           Entity added to each Level; HUD-tagged, global, renders the keys
      KeyLayout.cs            grid definition (row, col, width-in-units, action, label source)
      KeyState.cs             per-key "is pressed" from Celeste input
      SlantedKeyRenderer.cs   draws a parallelogram key with outline + rounded corners
      KeyLabels.cs            turns a Keys/Buttons binding into a short label (W, ↑, LSHIFT...)
    Settings/
      ColorPreset.cs          colour preset enum + hex parsing
  Dialog/
    English.txt               menu strings (MODOPTIONS_ZKO_*)
  Graphics/ (only if we go texture-based, see §4)
```

### How it hooks into the game
1. `ZKOModule.Load()` hooks `On.Celeste.Level.LoadLevel` and adds one `KeyOverlay` entity to the level if it isn't already there. Tags: `Tags.HUD | Tags.Global | Tags.PauseUpdate | Tags.TransitionUpdate`, so it survives room transitions and draws above gameplay at 1920×1080 HUD resolution.
2. `KeyOverlay.Update()` reads the input state each frame. `Render()` draws keys at the computed anchor.
3. `Unload()` removes the hook. No IL hooks needed.

### "Just works" with the player's bindings
- Actions use Celeste's own `VirtualButton`s: `Input.Dash`, `Input.CrouchDash`, `Input.Grab`, `Input.Jump`, `Input.ESC` / `Input.Pause`. `.Check` already respects every keyboard *and* controller binding, so remapping is automatic.
- Directions: `Input.MoveX` / `Input.MoveY` hide one direction when both are held (newest wins), which looks wrong on an overlay. Instead we read `Settings.Instance.Left/Right/Up/Down` bindings directly and check each one (`Binding.Check(Input.Gamepad, threshold)`), so holding A and D lights both.
- Labels for direction keys come from the first keyboard key in each binding (`Settings.Instance.Up.Keyboard[0]` → "W"). Re-read when the bindings change (cheap to re-read every few frames or on `OuiKeyboardConfig` close).

---

## 4. Rendering approach

**Recommended: procedural drawing.** Each key is drawn as a stack of 1px-high `Draw.Rect` strips, each shifted sideways by the slant, giving a clean parallelogram at any scale and any colour. Outline = draw the shape in the outline colour, then a slightly inset copy in the fill colour. Corners are rounded by trimming the first/last few strips. Labels use `ActiveFont` (Celeste's HUD font), drawn centred with a matching skew feel.
- Pros: colours/scale fully configurable, no art assets, crisp at any size.
- Cost: around 1-2k rects per frame, which is negligible.

Alternative: ship a white 9-slice key texture in `Graphics/Atlases/Gui/ZKO/` and tint it. Looks closer to the mock-up's soft corners but stretching a slanted 9-slice is awkward. Keep as fallback if the procedural look isn't good enough.

Positioning: compute overlay width/height from the grid × scale, then anchor to the chosen corner with padding (bottom-left default, 5% up from the bottom edge).

---

## 5. Milestones

| # | Milestone | Done when |
|---|---|---|
| M1 | Project scaffold | `dotnet build` produces `ZKO.dll`; Everest loads the mod; it shows in Mod Options |
| M2 | Static overlay | The 4×3 slanted grid draws bottom-left in a level, matching design.png layout |
| M3 | Live input | Every key lights on press via the player's bindings; both opposite directions can light at once; controller presses also light keys |
| M4 | Dynamic labels | Direction keys show the bound key names, and update after rebinding |
| M5 | Settings | Show/hide + hotkey, position presets, absolute X/Y, scale, colours, opacity, pause visibility all working and saved |
| M6 | Polish & package | Visual pass vs. design.png, no errors in `log.txt`, release zip (`everest.yaml` + dll + Dialog) ready to drop into `Mods/` |

---

## 6. Build & test setup

- Needs: .NET SDK 8, Celeste with Everest installed, and the path to the Celeste install (for the template's `CelestePrefix`).
- Build/test runs locally: `dotnet build` and copy the output into `Celeste/Mods/ZKO`.
- Manual test checklist: rebinding Dash/Jump in Options updates the overlay; WASD and arrow bindings both label correctly; overlay survives room transitions, death, pause, chapter restart; each position preset; hotkey hides/shows.

---

## 7. Risks

| Risk | Mitigation |
|---|---|
| Crouch Dash only exists on Celeste 1.4+ / current Everest | Target current Everest; if `Input.CrouchDash` is missing, hide CD key |
| Direction labels for odd keys (`OemSemicolon`, `LeftShift`) are long | Short-name table in `KeyLabels.cs`, fall back to the first 3 characters |
| Controller-only players get no meaningful direction labels | Show arrow glyphs when no keyboard key is bound |
| Overlay clashes with other HUD mods (speedrun timer is top-left) | Default is bottom-left; presets + absolute position let users move it |
| Building requires the local Celeste install path | Ask for it once at the start of implementation |

---

## 8. Decisions (2026-11-15)

1. **ESC** goes in the bottom-left slot.
2. **Settings order** applies within ZKO's own options page. ZKO's position in Everest's mod list is wherever Everest puts it.
3. **Direction labels** show the actually bound key (arrow keys draw an arrow glyph, also used when no keyboard key is bound).
4. **Colours:** full in-menu RGB editor (exact R/G/B sliders, 0 to 255) plus a preset list, for fill, outline, pressed and label colours. Each colour is a collapsible sub-menu so the options list stays short. Hex is also stored in the settings file.
5. **Where it shows:** new setting "Show overlay in": Gameplay only / Gameplay + pause menu (default) / Everywhere (also overworld and menus). Replaces "Show during pause menu".
6. **Git:** `git init` the folder.
7. **Bonus, invert mode:** separate On/Off toggles to invert the outline, the key fill, and the labels. Inverted parts use an inverting blend state (`src*(1-dst) + dst*(1-src)`, drawn in white) so they flip whatever is behind them, like Minecraft's cursor. Pressed colour is never inverted, so presses stay obvious. Opacity blends between the background and its inverse.

### Implementation notes that changed from §3/§4
- To make invert mode work, the overlay is drawn after the whole frame is composed (hook on `Monocle.Engine.RenderCore`, postfix) straight onto the back buffer, instead of as a HUD entity. Celeste's HUD can render into a transparent buffer at sub-1080p resolutions, where an inverting blend would have nothing to invert. Side effect: the overlay sits above the pause menu, which doubles as a live preview while editing its options.
- Shapes are drawn in real screen pixels (1px strips), so outline and fill never overlap, which inverting requires.
- Toggle hotkey is checked in a `Monocle.Engine.Update` postfix.
- Targets .NET 8 / Everest 5935+ (what current Everest and the official mod template use). The csproj references `Celeste.dll`, `MMHOOK_Celeste.dll` and `FNA.dll` from the Celeste folder directly, installs into `Mods/ZKO` after each build, and a Release build writes `ZKO.zip`.
- Shape edges are anti-aliased: a pixel that straddles an edge is drawn once with the coverage-weighted colour.
- Verified so far: compiles cleanly against the latest stable Everest's reference assemblies (build 6696); key geometry checked with an offline simulation. In-game check is still to do.
