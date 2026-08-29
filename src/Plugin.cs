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

        private ConfigEntry<KeyboardShortcut>? _toggleKey;

        private void Awake()
        {
            Log = Logger;
            // Unbound by default — no key to guess that won't collide with something else on some
            // setup. Shows up as a normal keybind picker in the F1 BepInEx Configuration Manager
            // menu if installed; otherwise editable directly in the generated .cfg file.
            _toggleKey = Config.Bind("Keybinds", "ToggleCinematicCockpitCamera", new KeyboardShortcut(),
                "Toggles Cinematic Cockpit Camera: hides the HUD and cockpit interior (dashboard, " +
                "canopy frame, control stick) while staying in the normal first-person cockpit view " +
                "— not the game's own free camera. Persists across camera switches and respawns " +
                "until toggled off again.");
            Log.LogInfo("NOCCC loaded.");
        }

        private void Update()
        {
            if (_toggleKey!.Value.IsDown()) CinematicView.Toggle();
            CinematicView.Tick(Time.deltaTime);
        }
    }
}
