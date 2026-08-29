namespace NOCCC.Tests
{
    public class CinematicViewPolicyTests
    {
        [Fact]
        public void Idle_when_never_activated()
        {
            bool forcedOff = false;

            var action = CinematicViewPolicy.Evaluate(active: false, inCockpitView: false, ref forcedOff);

            Assert.Equal(CinematicViewPolicy.Action.Idle, action);
            Assert.False(forcedOff);
        }

        [Fact]
        public void Entering_cockpit_view_while_active_hides_once_then_stays_hidden()
        {
            bool forcedOff = false;

            var first = CinematicViewPolicy.Evaluate(active: true, inCockpitView: true, ref forcedOff);
            Assert.Equal(CinematicViewPolicy.Action.EnterHidden, first);
            Assert.True(forcedOff);

            var second = CinematicViewPolicy.Evaluate(active: true, inCockpitView: true, ref forcedOff);
            Assert.Equal(CinematicViewPolicy.Action.StayHidden, second);
            Assert.True(forcedOff);
        }

        [Fact]
        public void Toggling_off_while_still_in_cockpit_view_restores()
        {
            // Regression case: the first version of this feature only ever called
            // SetCockpitRenderers(false) while Active, so toggling off without also leaving cockpit
            // view left the interior hidden forever.
            bool forcedOff = false;
            CinematicViewPolicy.Evaluate(active: true, inCockpitView: true, ref forcedOff);

            var action = CinematicViewPolicy.Evaluate(active: false, inCockpitView: true, ref forcedOff);

            Assert.Equal(CinematicViewPolicy.Action.Restore, action);
            Assert.False(forcedOff);
        }

        [Fact]
        public void Leaving_cockpit_view_while_still_active_restores()
        {
            bool forcedOff = false;
            CinematicViewPolicy.Evaluate(active: true, inCockpitView: true, ref forcedOff);

            var action = CinematicViewPolicy.Evaluate(active: true, inCockpitView: false, ref forcedOff);

            Assert.Equal(CinematicViewPolicy.Action.Restore, action);
            Assert.False(forcedOff);
        }

        [Fact]
        public void Restore_only_fires_once_per_transition()
        {
            bool forcedOff = false;
            CinematicViewPolicy.Evaluate(active: true, inCockpitView: true, ref forcedOff);
            CinematicViewPolicy.Evaluate(active: false, inCockpitView: false, ref forcedOff);

            var action = CinematicViewPolicy.Evaluate(active: false, inCockpitView: false, ref forcedOff);

            Assert.Equal(CinematicViewPolicy.Action.Idle, action);
            Assert.False(forcedOff);
        }

        [Fact]
        public void Reentering_cockpit_view_after_a_restore_hides_again()
        {
            bool forcedOff = false;
            CinematicViewPolicy.Evaluate(active: true, inCockpitView: true, ref forcedOff);
            CinematicViewPolicy.Evaluate(active: true, inCockpitView: false, ref forcedOff);

            var action = CinematicViewPolicy.Evaluate(active: true, inCockpitView: true, ref forcedOff);

            Assert.Equal(CinematicViewPolicy.Action.EnterHidden, action);
            Assert.True(forcedOff);
        }
    }
}
