using UnityEngine;

namespace GolfMayhem.ChaosEventSystem.Events
{
    /// <summary>
    /// CHAOS EVENT: Gravity Flip
    /// Temporarily inverts or dramatically amplifies gravity,
    /// sending every ball flying unpredictably across the course.
    /// </summary>
    public class GravityFlipEvent : ChaosEvent
    {
        private Vector3 _originalGravity;

        public override string DisplayName    => "Gravity Flip";
        public override string WarnMessage    => "⚠️ GRAVITY IS ABOUT TO MISBEHAVE...";
        public override string ActivateMessage => "🌀 GRAVITY FLIP! HOLD ON TO YOUR CLUBS!";
        public override float  Weight         => Configuration.WeightGravityFlip.Value;
        public override bool   IsEnabled      => Configuration.EnableGravityFlip.Value;

        public override void OnActivate()
        {
            _originalGravity = Physics.gravity;

            // Flip gravity on Y axis and apply the configured multiplier
            Physics.gravity = new Vector3(
                _originalGravity.x,
                Configuration.GravityFlipMultiplier.Value,
                _originalGravity.z
            );

            GolfMayhemPlugin.Log.LogInfo($"[GravityFlip] Gravity set to {Physics.gravity}");
        }

        public override void OnDeactivate()
        {
            Physics.gravity = _originalGravity;
            GolfMayhemPlugin.Log.LogInfo($"[GravityFlip] Gravity restored to {Physics.gravity}");
        }
    }
}
