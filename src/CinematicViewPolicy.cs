namespace NOCCC
{
    // Pure edge-triggered state machine for CinematicView.Tick. Cinematic View wants to hide the
    // cockpit interior only while both Active and actually in cockpit view, but the side-effecting
    // calls (SetCockpitRenderers/DisableExterior/FlightHud.EnableCanvas(false)) must fire once per
    // transition, not every tick — see CinematicView.Tick's own reassertion-policy comment. Extracted
    // so that edge case (bug: toggling off without leaving cockpit view never restored the interior)
    // can be covered by a test without a live Aircraft/CameraStateManager.
    internal static class CinematicViewPolicy
    {
        internal enum Action
        {
            Idle,
            EnterHidden,
            StayHidden,
            Restore,
        }

        // The caller owns forcedOff so it can keep storing it beside its other per-aircraft state;
        // this just decides what changes and what to do about it.
        internal static Action Evaluate(bool active, bool inCockpitView, ref bool forcedOff)
        {
            bool wantHidden = active && inCockpitView;
            if (wantHidden)
            {
                if (forcedOff) return Action.StayHidden;
                forcedOff = true;
                return Action.EnterHidden;
            }

            if (!forcedOff) return Action.Idle;
            forcedOff = false;
            return Action.Restore;
        }
    }
}
