using UnityEngine;

namespace GolfMayhem.ChaosEventSystem.Events
{
    public class GravityFlipEvent : ChaosEvent
    {
        private Vector3 _originalGravity;

        public override string DisplayName => "Gravity Flip";
        public override string WarnMessage => "Gravity is about to misbehave...";
        public override string ActivateMessage => "All players now have less gravity!";
        public override float Weight => Configuration.WeightGravityFlip.Value;
        public override bool IsEnabled => Configuration.EnableGravityFlip.Value;

        public override void OnActivate()
        {
            _originalGravity = Physics.gravity;
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
