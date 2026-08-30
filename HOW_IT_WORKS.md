# How NOCCC Works

NOCCC hides everything that would normally clutter the cockpit view — dashboard, canopy frame,
control stick, gun turret, rotor blades, the duplicate ejection-seat geometry — while staying in the
game's real first-person cockpit camera. It ships two independent techniques for clearing the
harder pieces (turret, rotor, hull), bound to separate keybinds so either can be used on its own.

Both techniques are entirely local and visual. Neither touches the aircraft's physics, its network
state, or anything another player sees — every change described below only affects what renders on
your own screen.

## The shared half: hiding the dashboard and canopy

Nuclear Option's own `Aircraft` class already ships built for this swap. It holds two arrays of
`Renderer`s — `cockpitRenderers` (dashboard, canopy frame, control stick, throttle, the interior
canopy glass) and `exteriorRenderers` (simplified stand-ins for the same canopy frame/glass, used
when the camera is outside the cockpit, e.g. chase view). `Aircraft.SetCockpitRenderers(bool)` is a
public method that flips one array on and the other off — the game calls it itself every time the
camera enters or leaves cockpit view.

NOCCC calls `SetCockpitRenderers(false)` while active, same as the game does when *leaving* the
cockpit — except NOCCC does this while *staying* in the cockpit camera. That alone would leave the
exterior stand-ins turned on, which are just as visible from inside the cockpit as the interior copies
they replace (they're not a real closed hull, they're the same canopy frame at lower detail). So NOCCC
also disables the exterior array directly, via reflection — `exteriorRenderers` is a private field,
not part of the public API, so this is the one place the mod reaches past what `Aircraft` exposes on
purpose.

The flight HUD is hidden the same way the game's own `FlightHud.EnableCanvas(bool)` already works —
NOCCC just calls it with `false` and keeps re-asserting it every frame, since the game's own code can
re-enable it on its own schedule.

## Technique 1: pushing the camera (default keybind)

Instead of finding and hiding the hull, turret, rotor, and ejection-capsule geometry individually,
this technique moves the camera's eye point forward, physically past all of it.

Nuclear Option's `CameraCockpitState` (the class that drives the cockpit camera every frame) sets the
camera's local position and rotation unconditionally each frame — position from pan/tilt/TrackIR
input, rotation from the same. Whatever position it computes gets overwritten again next frame, so
there's no way to move the camera by editing that state directly; instead, NOCCC uses
[Harmony](https://github.com/BepInEx/HarmonyX) (already bundled with BepInEx) to patch
`CameraCockpitState.UpdateState` with a **postfix** — code that runs immediately after the game's own
method body finishes, once per frame:

```csharp
[HarmonyPatch(typeof(CameraCockpitState), "UpdateState")]
private static void Postfix(CameraStateManager cam)
{
    if (!CinematicView.CameraPushActive) return;
    cam.transform.localPosition += Vector3.forward * offset;
}
```

The offset is added in the camera's *local* space, relative to its parent (`cameraPivot`, which
tracks the aircraft's own orientation, not the pilot's head). That distinction matters: adding along
`Vector3.forward` here means "forward along the nose of the plane," a fixed point in space regardless
of where the pilot is currently looking. An earlier version of this patch instead added along
`cam.transform.localRotation * Vector3.forward` — the direction the pilot is *currently looking* —
which worked staring straight ahead but let the nose creep back into view the moment the pilot tilted
their head down, since the push direction tilted with it. Pan, tilt, and TrackIR all still work
normally from the new, moved-forward vantage point — only the vantage point itself is fixed.

The push distance is a single number (`ForwardOffset`, 0–10 meters, set in the F1 menu). One number
has to clear every obstruction on that airframe at once — a fixed-wing nose is one thing, a
helicopter with a chin-mounted gun turret sitting well forward of it is another. Too short and
something still edges into frame; too far and the camera either passes out past the front of the
aircraft into its exterior hull, or clips into whatever geometry sits further forward.

## Technique 2: hiding each renderer (the other keybind)

This is the more traditional approach: find every piece of geometry that would obstruct the view, and
disable its `Renderer` component directly, leaving the camera wherever the game puts it.

The obstacle is that most of those pieces are not simple children of the aircraft in Unity's scene
hierarchy — walking `Aircraft`'s own transform hierarchy only reaches the main airframe mesh. Three
separate discoveries were needed to reach everything else, each confirmed by testing in-game rather
than assumed from the game's source:

- **The main airframe hull** (`trainer`, `AttackHelo1`, the gun pod, its barrel) — these *are*
  ordinary children of the aircraft, found by an explicit name list. A name list rather than "hide
  everything under the aircraft" because that same hierarchy also holds functional parts (gear doors,
  wheels, warning lights, muzzle flash) that must stay under the game's own control.
- **The ejection-seat capsule** (`Aircraft.cockpit`) — a public field pointing at a second, complete
  copy of the cockpit/canopy geometry, kept so the ejected capsule still looks intact once it
  separates. Normally invisible only because it perfectly overlaps the primary copy.
- **The gun turret and the EO/TGP sensor ball** (`Turret`, `Aircraft.targetCam`) and **the rotor
  assembly** (`RotorShaft`, main and tail rotor) — each is its own component elsewhere in the scene,
  linked back to the aircraft by a plain reference field rather than by Unity parenting (`Turret`
  exposes `GetAttachedUnit()`; `RotorShaft` has a public `aircraft` field). NOCCC scans every instance
  of each type in the scene and keeps the ones whose reference points at the player's own aircraft.
- **Whatever's left** — a proximity fallback catches anything the above misses (confirmed necessary
  for a second-seat's own duplicate cockpit module on a two-seat trainer): any enabled renderer within
  3 meters of the camera, except an explicit list of parts that must stay under the game's own control
  (the pilot's own body, gear/weapon doors, muzzle flash).

Every renderer found this way gets `Renderer.enabled = false` while the mode is active, and `= true`
the moment it's turned off or the camera leaves cockpit view. This is more reliable than the camera
push — nothing is ever actually in frame to clip through — but only for aircraft this discovery
process has already been done for. Right now that's the Trainer and the Chicane.

## Timing: why "on/off" isn't just a boolean check

Both techniques restore themselves not just when toggled off, but the moment cockpit view is left at
all (switching to chase cam, external view, the map) — nothing else in the game will ever turn them
back on, unlike the native interior/exterior split, which the game already restores on its own when
leaving the cockpit. A small state machine (`CinematicViewPolicy`) tracks this transition explicitly
(`EnterHidden` / `StayHidden` / `Restore`) rather than re-deriving it from raw booleans every frame,
because one specific action — `SetCockpitRenderers` — has an audible side effect (it also toggles
each engine's interior/exterior sound mix) and must fire exactly once per transition, not every frame;
calling it continuously produced audible cutting and saturation during testing. The camera push and
the per-renderer hide have no such side effect, so they simply reassert their state every frame while
active — a Renderer's `enabled` flag or a transform's position have no cost to set redundantly.

## What NOCCC touches, in one list

- **Public game API**: `Aircraft.SetCockpitRenderers`, `Aircraft.cockpit`, `Aircraft.targetCam`,
  `Turret.GetAttachedUnit()`, `RotorShaft.aircraft`, `FlightHud.EnableCanvas`, `GameManager`,
  `SceneSingleton<T>`, `CameraStateManager`, `CameraCockpitState`.
- **One private field via reflection**: `Aircraft.exteriorRenderers` — read-only access to flip its
  contents off; nothing is written back to the aircraft's own state.
- **One Harmony patch**: a postfix on `CameraCockpitState.UpdateState`, adding a local position offset
  after the game's own per-frame camera placement runs.
- **Nothing else**: no networking calls, no physics, no changes to collision, damage, or anything
  visible to other players — every effect described above is local rendering state on the client
  running NOCCC.
