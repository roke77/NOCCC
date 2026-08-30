using System.Reflection;
using UnityEngine;

namespace NOCCC
{
    // Fully unobstructed first-person view: the flight HUD and the cockpit interior both hidden,
    // while remaining in the game's normal first-person cockpit camera — not the game's own free
    // camera. Requested by a player who wanted clean footage while actually flying ("camera control
    // is nothing like flying a chicane").
    //
    // Extracted from NOXMFD (github.com/roke77/NOXMFD, issue #72, docs/cinematic-view.md), where this
    // was one feature among many sharing a HUD-hide mechanism with a separate Power feature. Here it's
    // the only feature, so both halves (HUD + 3D cockpit renderers) live in one class instead of two.
    //
    // Two independent, separately-keybound mechanisms clear the hull/turret/rotor/ejection-capsule
    // obstructions NOXMFD's four-source ownership scan used to hunt down individually:
    //  - CameraPushActive (ToggleCinematicCockpitCamera): pushes the camera forward past the
    //    obstruction instead of finding it (see HarmonyPatches.cs). No per-aircraft renderer discovery
    //    at all, but the one free parameter (Plugin.ForwardOffset) needs hand-tuning per airframe.
    //  - RendererHideActive (ToggleHideCockpit): the original NOXMFD approach, extracted into
    //    StructuralRendererHide.cs — finds and hides each obstruction's own renderer.
    // Both can be on at once (harmless — RendererHideActive just also hides whatever the camera push
    // didn't clear), which is deliberate: it makes comparing the two, or falling back to the reliable
    // one, a single extra keypress rather than a rebuild.
    //
    // The native interior/exterior renderer split (SetCockpitRenderers/DisableExterior) and HUD hide
    // are shared by both mechanisms — they already handle the dashboard/canopy/stick correctly and for
    // free, regardless of which mechanism is clearing the rest.
    internal static class CinematicView
    {
        internal static bool CameraPushActive { get; private set; }
        internal static bool RendererHideActive { get; private set; }

        // Both mechanisms additionally have to restore the moment cockpit view is left at all (chase
        // cam, external, map) — not just on toggle-off — since nothing else in the game will ever turn
        // them back on for us the way CameraCockpitState.LeaveState already does for the native
        // interior/exterior arrays. Leaving either off past that point would make the aircraft
        // invisible from chase cam/other players (RendererHideActive) or leave the camera stuck
        // forward of where the native code expects it (CameraPushActive). Two independent trackers,
        // since the two mechanisms can be toggled independently of each other — see the class comment.
        private static bool _forcedOff;
        private static bool _rendererForcedOff;

        private static bool _reflectionTried;
        private static FieldInfo? _exteriorField;

        internal static void ToggleCameraPush() => CameraPushActive = !CameraPushActive;
        internal static void ToggleRendererHide() => RendererHideActive = !RendererHideActive;

        // Called every frame from Plugin.Update. SetCockpitRenderers/DisableExterior (audio-affecting:
        // SetCockpitRenderers also flips IEngine.SetInteriorSounds on every engine) fire only once per
        // actual state *transition* (CinematicViewPolicy.Action.EnterHidden/Restore) — calling
        // SetCockpitRenderers every tick produced audible cutting/saturation, since unlike a canvas
        // SetActive, repeating it isn't a no-op. FlightHud.EnableCanvas(false) still needs reasserting
        // every tick to win over the game's own native re-enable, so it's not edge-triggered.
        //
        // StructuralRendererHide.SetVisible gets its own, separate Evaluate call below: it has no
        // audio/side-effect cost (plain Renderer.enabled), so it's driven purely by RendererHideActive
        // rather than the interior/exterior split's "is either mechanism on" — that keeps the (only
        // meaningful when RendererHideActive is actually used) renderer scan from ever running while
        // only CameraPushActive is in play.
        internal static void Tick(float dt)
        {
            if (!GameManager.GetLocalAircraft(out Aircraft ac) || ac == null) return;
            CameraStateManager? cam = SceneSingleton<CameraStateManager>.i;
            bool inCockpitView = cam != null && cam.currentState == cam.cockpitState;
            bool anyMechanismActive = CameraPushActive || RendererHideActive;

            switch (CinematicViewPolicy.Evaluate(anyMechanismActive, inCockpitView, ref _forcedOff))
            {
                case CinematicViewPolicy.Action.EnterHidden:
                    // SetCockpitRenderers(false) alone isn't enough: it deliberately turns the
                    // *exterior* array on, and that array isn't a full outer hull — it's simplified
                    // LOD stand-ins for the same canopy frame/glass (canopyframe_F_simple etc.),
                    // just as visible from inside the cockpit as the detailed version they replace.
                    // Cinematic View wants both arrays off, not the normal interior/exterior split.
                    ac.SetCockpitRenderers(false);
                    DisableExterior(ac);
                    FlightHud.EnableCanvas(false);
                    break;
                case CinematicViewPolicy.Action.StayHidden:
                    FlightHud.EnableCanvas(false);
                    break;
                case CinematicViewPolicy.Action.Restore:
                    // Only needs restoring if we're still in cockpit view — leaving it already put
                    // the native interior/exterior split and HUD visibility back correctly on its
                    // own, via CameraCockpitState.LeaveState/GameplayUI.
                    if (inCockpitView)
                    {
                        ac.SetCockpitRenderers(true);
                        FlightHud.EnableCanvas(true);
                    }
                    break;
            }

            switch (CinematicViewPolicy.Evaluate(RendererHideActive, inCockpitView, ref _rendererForcedOff))
            {
                case CinematicViewPolicy.Action.EnterHidden:
                case CinematicViewPolicy.Action.StayHidden:
                    StructuralRendererHide.SetVisible(ac, false, cam);
                    break;
                case CinematicViewPolicy.Action.Restore:
                    StructuralRendererHide.SetVisible(ac, true, cam);
                    break;
            }
        }

        private static void DisableExterior(Aircraft ac)
        {
            if (!_reflectionTried)
            {
                _reflectionTried = true;
                _exteriorField = typeof(Aircraft).GetField("exteriorRenderers", BindingFlags.NonPublic | BindingFlags.Instance);
                if (_exteriorField == null)
                    Plugin.Log?.LogWarning("[NOCCC] CinematicView: could not find Aircraft.exteriorRenderers.");
            }
            if (_exteriorField?.GetValue(ac) is not Renderer[] renderers) return;
            foreach (Renderer r in renderers)
                if (r != null) r.enabled = false;
        }
    }
}
