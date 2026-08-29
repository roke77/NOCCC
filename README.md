# NOCCC — Nuclear Option Cinematic Cockpit Camera

A [BepInEx](https://github.com/BepInEx/BepInEx) 5 mod for [Nuclear Option](https://store.steampowered.com/app/2168680/Nuclear_Option/)
that hides the flight HUD and the cockpit interior (dashboard, canopy frame, control stick, gun
turret, rotor blades) while staying in the game's normal first-person cockpit camera — not the
game's own free/spectator camera. For recording clean cockpit footage while actually flying.

## Install

1. Install [BepInEx 5](https://github.com/BepInEx/BepInEx) for Nuclear Option, if you haven't already.
2. Drop `NOCCC.dll` into `<Nuclear Option install>\BepInEx\plugins\`.
3. Launch the game once — this generates `BepInEx\config\com.roque.NOCCC.cfg`.

## Usage

The toggle keybind ships **unbound** so it can't collide with anything else on your setup. Bind it
one of two ways:

- **F1 in-game menu** — if you have [BepInEx.ConfigurationManager](https://github.com/BepInEx/BepInEx.ConfigurationManager)
  installed (bundled with most Nuclear Option modpacks), press F1 in-game, find **NOCCC**, and click
  the keybind field next to *Toggle Cinematic Cockpit Camera*, then press the key you want.
- **Edit the config file directly** — open `BepInEx\config\com.roque.NOCCC.cfg` in a text editor
  and set `ToggleCinematicCockpitCamera` under `[Keybinds]` (e.g. `F9`), then restart the game.

Press the bound key while flying, in cockpit view, to toggle the unobstructed view on and off. It
stays on across camera switches and respawns until you press it again.

## Known limitations

The cockpit-interior allowlist is confirmed only on the **Trainer** and the **Chicane** attack
helicopter. Other aircraft may still show some interior geometry the first time they're flown with
this on — if you find one, please open an issue with the aircraft name and a screenshot.

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
