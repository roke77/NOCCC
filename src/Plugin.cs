using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using UnityEngine;

namespace NOCCC
{
    [BepInPlugin("com.roque.NOCCC", "NOCCC", MyPluginInfo.PLUGIN_VERSION)]
    [BepInProcess("NuclearOption.exe")]
    [BepInProcess("NuclearOptionServer.exe")]
    public class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource? Log;

        // Read every frame by HarmonyPatches' camera-push postfix — how far forward (meters) to move
        // the eye point while that mode is active. An AcceptableValueList renders as a dropdown in the
        // F1 Configuration Manager menu instead of a free-typed number, so a tester can't enter a
        // value we haven't actually tried.
        internal static ConfigEntry<float>? ForwardOffset;

        private ConfigEntry<KeyboardShortcut>? _cameraPushKey;
        private ConfigEntry<KeyboardShortcut>? _hideCockpitKey;

        private void Awake()
        {
            Log = Logger;
            // Unbound by default — no key to guess that won't collide with something else on some
            // setup. Shows up as a normal keybind picker in the F1 BepInEx Configuration Manager
            // menu if installed; otherwise editable directly in the generated .cfg file.
            _cameraPushKey = Config.Bind("Keybinds", "ToggleCinematicCockpitCamera", new KeyboardShortcut(),
                "Toggles Cinematic Cockpit Camera: hides the HUD and cockpit interior (dashboard, " +
                "canopy frame, control stick) while staying in the normal first-person cockpit view " +
                "— not the game's own free camera. Pushes the camera forward past the nose instead of " +
                "hiding it (see ForwardOffset). Persists across camera switches and respawns until " +
                "toggled off again.");
            _hideCockpitKey = Config.Bind("Keybinds", "ToggleHideCockpit", new KeyboardShortcut(),
                "Alternate to ToggleCinematicCockpitCamera: hides the HUD and cockpit interior the same " +
                "way, but clears the hull/turret/rotor/ejection-capsule obstructions by finding and " +
                "hiding each one's renderer instead of moving the camera. Can be toggled independently " +
                "of ToggleCinematicCockpitCamera — useful for comparing the two side by side.");
            ForwardOffset = Config.Bind("Settings", "ForwardOffset", 3f,
                new ConfigDescription(
                    "Meters to push the camera forward (past the nose) while Cinematic Cockpit Camera " +
                    "is active. 0 disables the push. Tune per aircraft — a helicopter's chin-mounted " +
                    "turret needs a different distance than a fixed-wing nose.",
                    new AcceptableValueList<float>(0f, 1f, 2f, 3f, 4f, 5f, 6f, 7f, 8f, 9f, 10f)));
            HarmonyPatches.Init();
            Log.LogInfo("NOCCC loaded.");
        }

        private void Update()
        {
            if (_cameraPushKey!.Value.IsDown()) CinematicView.ToggleCameraPush();
            if (_hideCockpitKey!.Value.IsDown()) CinematicView.ToggleRendererHide();
            CinematicView.Tick(Time.deltaTime);
        }
    }
}
