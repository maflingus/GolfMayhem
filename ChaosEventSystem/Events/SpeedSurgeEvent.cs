using UnityEngine;

namespace GolfMayhem.ChaosEventSystem.Events
{
    public class SpeedSurgeEvent : ChaosEvent
    {
        public override string DisplayName => "Speed Surge";
        public override string WarnMessage => "Something is accelerating...";
        public override string ActivateMessage => "Everyone is now faster!";
        public override float Weight => Configuration.WeightSpeedSurge.Value;
        public override bool IsEnabled => Configuration.EnableSpeedSurge.Value;

        public override void OnActivate()
        {
            var balls = Object.FindObjectsByType<GolfBall>(FindObjectsSortMode.None);
            float multiplier = Configuration.SpeedSurgeMultiplier.Value;
            int count = 0;

            foreach (var ball in balls)
            {
                if (ball == null || ball.Rigidbody == null) continue;
                ball.Rigidbody.linearVelocity *= multiplier;
                ball.Rigidbody.angularVelocity *= multiplier;
                count++;
            }

            GolfMayhemPlugin.Log.LogInfo($"[SpeedSurge] Launched {count} balls at x{multiplier} speed.");
        }

        public override void OnDeactivate()
        {
            GolfMayhemPlugin.Log.LogInfo("[SpeedSurge] Deactivated.");
        }

        public override void OnUpdate()
        {
        }
    }
}
