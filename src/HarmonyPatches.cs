using HarmonyLib;
using UnityEngine;

namespace NOCCC
{
    // Harmony (HarmonyX) is already a transitive compile/runtime dependency of BepInEx.Core, so no
    // new .csproj reference or install step is needed to use it.
    internal static class HarmonyPatches
    {
        internal static void Init()
        {
            new Harmony("com.roque.NOCCC").PatchAll(typeof(HarmonyPatches).Assembly);
        }

        // CinematicView.CameraPushActive's mechanism: instead of finding and hiding each obstruction
        // (see StructuralRendererHide.cs, the other mechanism), push the eye point forward past it,
        // along the aircraft's own nose direction, far enough that the obstruction ends up behind the
        // camera.
        //
        // CameraCockpitState.UpdateState sets cam.transform.localPosition/localRotation
        // unconditionally every frame (confirmed by decompiling it — the game's own TrackIR support
        // already leans the same local-Z axis up to 0.45m for head lean), so a postfix here is the
        // only place a further offset survives the frame — anything applied earlier just gets
        // overwritten.
        //
        // Deliberately plain Vector3.forward, not cam.transform.localRotation * Vector3.forward: the
        // first attempt used the rotated version, which pushes along wherever the pilot is currently
        // *looking* — fine staring straight ahead, but tilting the view down swings the push toward
        // the ground instead of continuing past the nose, since the nose is fixed to the airframe, not
        // to the pilot's gaze. cam.transform's parent is cameraPivot, which tracks the aircraft's own
        // orientation with no pan/tilt applied — so a plain local Vector3.forward here is a fixed point
        // in space just ahead of the nose, from which pan/tilt/TrackIR then look around exactly as
        // before, matching the original ask ("push the camera forward, same centering as in cockpit").
        [HarmonyPatch(typeof(CameraCockpitState), "UpdateState")]
        private static class CameraCockpitState_UpdateState_Patch
        {
            private static void Postfix(CameraStateManager cam)
            {
                if (!CinematicView.CameraPushActive) return;
                float offset = Plugin.ForwardOffset?.Value ?? 0f;
                cam.transform.localPosition += Vector3.forward * offset;
            }
        }
    }
}
