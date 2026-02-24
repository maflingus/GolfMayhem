using UnityEngine;

namespace GolfMayhem.ChaosEventSystem.Events
{
    /// <summary>
    /// CHAOS EVENT: Speed Surge
    /// Every ball on the course suddenly gets massively accelerated.
    ///
    /// Uses the real GolfBall class from Assembly-CSharp.
    /// GolfBall.Rigidbody is a public property exposing the internal Rigidbody.
    /// Unity 6 uses linearVelocity (not .velocity which was removed).
    /// </summary>
    public class SpeedSurgeEvent : ChaosEvent
    {
        public override string DisplayName     => "Speed Surge";
        public override string WarnMessage     => "⚠️ SOMETHING IS ACCELERATING...";
        public override string ActivateMessage => "💨 SPEED SURGE! EVERYTHING IS FASTER!";
        public override float  Weight          => Configuration.WeightSpeedSurge.Value;
        public override bool   IsEnabled       => Configuration.EnableSpeedSurge.Value;

        public override void OnActivate()
        {
            // Use the real GolfBall class directly — no tag guessing needed.
            // GolfBall.Rigidbody is a public property: public Rigidbody Rigidbody => rigidbody;
            var balls = Object.FindObjectsByType<GolfBall>(FindObjectsSortMode.None);
            float multiplier = Configuration.SpeedSurgeMultiplier.Value;
            int count = 0;

            foreach (var ball in balls)
            {
                if (ball == null || ball.Rigidbody == null) continue;
                // Unity 6 API: linearVelocity replaces the old .velocity property.
                // GolfBall itself uses this in ApplyGravity(): rigidbody.linearVelocity += ...
                ball.Rigidbody.linearVelocity *= multiplier;
                ball.Rigidbody.angularVelocity *= multiplier;
                count++;
            }

            GolfMayhemPlugin.Log.LogInfo($"[SpeedSurge] Launched {count} balls at x{multiplier} speed.");
        }

        public override void OnDeactivate()
        {
            // One-shot impulse — physics damping will naturally slow balls down.
            // Nothing to reset.
            GolfMayhemPlugin.Log.LogInfo("[SpeedSurge] Deactivated.");
        }

        public override void OnUpdate()
        {
            // Intentionally empty: this is a one-shot event, not a continuous effect.
            // If you want sustained speed boost, add per-frame force here.
        }
    }
}
