using System.Collections.Generic;
using UnityEngine;

namespace NOCCC
{
    // CinematicView.RendererHideActive's mechanism (ToggleHideCockpit keybind): the hull/turret/rotor/
    // ejection-capsule half of Cinematic View, done the original NOXMFD way — find each obstruction
    // individually and disable its Renderer, rather than pushing the camera past it (the other
    // mechanism, see HarmonyPatches.cs). Slower to extend to a new aircraft (each one may need its own
    // entry in StructuralRendererNames) but doesn't depend on finding a forward distance that clears
    // every obstruction without clipping through anything solid.
    internal static class StructuralRendererHide
    {
        // Main-airframe structure (hull only) — unlike everything under Aircraft.cockpit (see
        // EnsureStructuralRenderers), the main airframe also carries functional/transient renderers
        // that must stay untouched, so this half stays an explicit allowlist rather than "hide
        // everything under ac". Neither the turret (gun barrel/mount) nor the rotors are here — both
        // are separate modules found by ownership, not name (see EnsureStructuralRenderers).
        // ponytail: hardcoded per aircraft type, not a generic classification — confirmed only on
        // the Trainer and the Chicane attack helicopter so far. Other aircraft may show extra
        // unhidden hull pieces until their own renderer names are added here.
        private static readonly string[] StructuralRendererNames =
        {
            "trainer", "fuselage_lod1", "gunpod", "trainer_gunpod_barrel", "AttackHelo1",
        };

        // Never disabled, even when found close to the camera or under another module's hierarchy:
        // weapon/gear functional elements that must stay untouched, plus "pilot" and "renderer_pilot"
        // (its Chicane-specific equivalent) — the game manages the pilot body's visibility itself
        // (TogglePilotVisibility, called by CameraCockpitState on every camera enter/leave), and
        // directly toggling its Renderer here fights that system, leaving the pilot's body visibly
        // out of place once Cinematic View turns back off.
        private static readonly HashSet<string> NeverHideNames = new HashSet<string>
        {
            "muzzle", "muzzleFlash", "weapondoor_L", "weapondoor_R", "geardoor_L", "geardoor_R",
            "geardoor_FL", "geardoor_FR", "ladder", "contactSparks", "gear_F_sprung",
            "gear_F_unsprung", "wheel_F", "gearlight_F", "chocks", "warningLightL", "warningLightR",
            "pilot", "renderer_pilot", "sun",
        };

        // A rear/second seat's own duplicate cockpit module (confirmed on a two-seat Trainer: flying
        // from the rear seat left cockpit_R/canopyframe_R/canopy_R_lod1/EjectionSeat all still
        // visible, none reachable via ac's or ac.cockpit's own hierarchy — Aircraft has only one
        // UnitPart cockpit field, seemingly always the front seat's) is exactly the kind of thing no
        // name list or hierarchy walk generalizes to. Instead of chasing every seat/module
        // permutation by name, this catches it by *position*: every enabled Renderer whose bounds
        // center falls within this radius of the camera, minus NeverHideNames, gets hidden too.
        private const float NearbyRadius = 3f;

        private static Aircraft? _structuralAircraft;
        private static Renderer[] _structuralRenderers = System.Array.Empty<Renderer>();

        internal static void SetVisible(Aircraft ac, bool visible, CameraStateManager? cam)
        {
            EnsureStructuralRenderers(ac, cam);
            foreach (Renderer r in _structuralRenderers)
                if (r != null) r.enabled = visible;
        }

        // Resolved once per aircraft instance (a respawn is a new GameObject, so the cache must key
        // off the instance, not just "have we ever resolved this before"). Combines every source
        // found necessary so far into one set — each covers a gap the others leave, since most of an
        // aircraft's "attachments" turned out not to be literal Transform children of ac at all (see
        // the per-source comments below for what was actually confirmed on the Trainer/Chicane).
        // ponytail: never re-scanned within the same aircraft instance's lifetime, so a module that
        // spawns or gets replaced (a rearmed weapon station, a rebuilt turret) after the first
        // Cinematic View activation on this aircraft would be missed until the next respawn. Upgrade
        // path if that's ever reported: drop the cache guard on CinematicView's EnterHidden transition
        // (see CinematicView.Tick), so each fresh activation re-scans instead of trusting the first one
        // for the aircraft's whole life.
        private static void EnsureStructuralRenderers(Aircraft ac, CameraStateManager? cam)
        {
            if (ReferenceEquals(_structuralAircraft, ac)) return;
            _structuralAircraft = ac;
            var found = new HashSet<Renderer>();

            // The main airframe: an explicit name allowlist (StructuralRendererNames), since this
            // hierarchy also holds functional renderers that must stay untouched — can't be a
            // blanket hide.
            foreach (Renderer r in ac.GetComponentsInChildren<Renderer>(true))
            {
                if (r == null) continue;
                foreach (string name in StructuralRendererNames)
                    if (r.name == name) { found.Add(r); break; }
            }

            // Aircraft.cockpit: an ejection-seat/capsule module with its own complete duplicate
            // canopy/cockpit geometry, needed so the pod looks intact once it separates, normally
            // invisible only because it perfectly overlaps the primary copy. A separate UnitPart,
            // not necessarily parented under the aircraft.
            AddChildRenderers(found, ac.cockpit);

            // Aircraft.targetCam: the chin-mounted EO/TGP gimbal ("targetCamForward") — a separate
            // mount from the gun Turret below, public field, so no ownership search needed.
            AddChildRenderers(found, ac.targetCam);

            // Turret (the ball-turret gun mount, confirmed on the Chicane): its own MonoBehaviour
            // module identified only by an attachedUnit *reference* back to the aircraft, not
            // Transform parenting, so no name in StructuralRendererNames could ever match it.
            // GetAttachedUnit() is the public lookup Turret itself exposes for this.
            foreach (Turret turret in Object.FindObjectsByType<Turret>(FindObjectsSortMode.None))
                if (turret != null && ReferenceEquals(turret.GetAttachedUnit(), ac))
                    AddChildRenderers(found, turret);

            // RotorShaft (main + tail rotor, confirmed on the Chicane): same "separate module" story
            // as Turret, but with an even more direct ownership link — a public `aircraft` field.
            foreach (RotorShaft rotor in Object.FindObjectsByType<RotorShaft>(FindObjectsSortMode.None))
                if (rotor != null && ReferenceEquals(rotor.aircraft, ac))
                    AddChildRenderers(found, rotor);

            // Proximity to the camera: catches whatever the sources above miss entirely — confirmed
            // necessary for a second/rear seat's own duplicate cockpit module (see NearbyRadius).
            if (cam != null && cam.mainCamera != null)
            {
                Vector3 camPos = cam.mainCamera.transform.position;
                foreach (Renderer r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                {
                    if (r == null || NeverHideNames.Contains(r.name)) continue;
                    if (Vector3.Distance(r.bounds.center, camPos) <= NearbyRadius) found.Add(r);
                }
            }

            _structuralRenderers = new List<Renderer>(found).ToArray();
        }

        // Shared by every hierarchy-walk source above: every Renderer under root except
        // NeverHideNames. root is null-checked here (rather than at each call site) since several
        // callers pass a possibly-absent module reference (ac.cockpit, ac.targetCam).
        private static void AddChildRenderers(HashSet<Renderer> found, Component? root)
        {
            if (root == null) return;
            foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true))
                if (r != null && !NeverHideNames.Contains(r.name)) found.Add(r);
        }
    }
}
