# NOCCC — Nuclear Option Cinematic Cockpit Camera

A [BepInEx](https://github.com/BepInEx/BepInEx) 5 mod for [Nuclear Option](https://store.steampowered.com/app/2168680/Nuclear_Option/)
that hides the flight HUD and the cockpit interior (dashboard, canopy frame, control stick, gun
turret, rotor blades) while staying in the game's normal first-person cockpit camera — not the
game's own free/spectator camera. For recording clean cockpit footage while actually flying.

See [HOW_IT_WORKS.md](HOW_IT_WORKS.md) for the technical details of both hiding techniques and what
game internals they touch.

## Install

1. Install [BepInEx 5](https://github.com/BepInEx/BepInEx) for Nuclear Option, if you haven't already.
2. Drop `NOCCC.dll` into `<Nuclear Option install>\BepInEx\plugins\`.
3. Launch the game once — this generates `BepInEx\config\com.roque.NOCCC.cfg`.

## Usage

Two independent keybinds, both ship **unbound** so neither collides with anything else on your
setup. Bind either (or both) one of two ways:

- **F1 in-game menu** — if you have [BepInEx.ConfigurationManager](https://github.com/BepInEx/BepInEx.ConfigurationManager)
  installed (bundled with most Nuclear Option modpacks), press F1 in-game, find **NOCCC**, and click
  the keybind field next to the one you want, then press the key you want.
- **Edit the config file directly** — open `BepInEx\config\com.roque.NOCCC.cfg` in a text editor
  and set the key under `[Keybinds]` (e.g. `F9`), then restart the game.

The two keybinds clear the same obstructions (hull, turret, rotor, ejection-capsule geometry) by
different means, and can be toggled independently of each other:

- **Toggle Cinematic Cockpit Camera** — pushes the camera forward past the obstruction. How far it
  pushes is set by **ForwardOffset**, also in the F1 menu under NOCCC's Settings (a 0–10 meter
  dropdown, default 3). No per-aircraft setup, but the right distance varies by airframe — too short
  and the nose or a chin-mounted turret still pokes into frame; too far and the camera pops out past
  the airframe entirely or clips into whatever's further forward.
- **Toggle Hide Cockpit** — hides each obstruction's own renderer instead of moving the camera. More
  reliable on aircraft the camera push doesn't clear cleanly, but only covers what's already been
  found and named for that airframe (see Known limitations).

Press either bound key while flying, in cockpit view, to toggle its effect on and off. Both persist
across camera switches and respawns until pressed again, independently of each other.

## Troubleshooting

**NOCCC doesn't appear in the F1 menu at all.** Check the game's log
(`BepInEx\LogOutput.log`) for `[Info : NOCCC] NOCCC loaded.` — if that line is there, NOCCC itself
loaded fine and the problem is on Configuration Manager's side, not NOCCC's.

**One or both keybind fields are missing, but the rest of NOCCC's section (ForwardOffset) shows up
fine.** Configuration Manager's own `BepInEx\config\com.bepis.bepinex.configurationmanager.cfg` has a
`[Filtering]` section with a **"Show keybinds"** setting — when `false`, it hides every keybind field
for every plugin, not just NOCCC's. Fix it either way:

- In-game: open the F1 window and check the **"Show keybinds"** checkbox near the search box, or
- Directly: set `Show keybinds = true` under `[Filtering]` in that cfg file (its own documented
  default), then reopen F1. If the game is running when you edit it, the change may get overwritten
  on exit — the in-game checkbox is the more reliable fix while playing.

## Known limitations

**Toggle Cinematic Cockpit Camera** (camera push): a single ForwardOffset has to clear every
obstruction on the airframe at once — the nose, and on a helicopter a chin-mounted turret sitting
well forward of it too. There's no guarantee one distance clears both without also clipping into
whichever one it passes closest to; if it doesn't work well on a given aircraft, try **Toggle Hide
Cockpit** instead.

**Toggle Hide Cockpit** (renderer hide): its allowlist of hull/turret/rotor pieces is confirmed only
on the **Trainer** and the **Chicane** attack helicopter. Other aircraft may still show some interior
geometry the first time they're flown with this on — if you find one, please open an issue with the
aircraft name and a screenshot.

## Building from source

Requires the .NET SDK and a Nuclear Option install.

```
git clone https://github.com/<your-fork>/NOCCC.git
cd NOCCC
dotnet build -c Release
```

If your game isn't installed at the default Steam path, create a `GameDir.props` next to
`NOCCC.csproj` (gitignored, machine-specific):

```xml
<Project><PropertyGroup>
  <GameDir>D:\SteamLibrary\steamapps\common\Nuclear Option</GameDir>
</PropertyGroup></Project>
```

Building also copies `NOCCC.dll` straight into `$(GameDir)\BepInEx\plugins`.

### Tests

The toggle/restore state machine (`CinematicViewPolicy`) is pure C# with no Unity dependency, and is
covered by `tools/tests`:

```
dotnet test tools/tests/NOCCC.Tests.csproj
```

## Credit

Extracted from [NOXMFD](https://github.com/roke77/NOXMFD)'s Cinematic View feature (issue #72),
where it originated as one feature among several. This repo is the same implementation, standalone,
with its own F1-menu keybind instead of NOXMFD's web-based keybind page.

## License

MIT — see [LICENSE](LICENSE).
