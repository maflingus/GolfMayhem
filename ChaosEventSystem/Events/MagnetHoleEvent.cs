using UnityEngine;

namespace GolfMayhem.ChaosEventSystem.Events
{
    public class MagnetHoleEvent : ChaosEvent
    {
        private const float REPEL_FORCE = 35f;
        private const float REPEL_RADIUS = 12f;

        public override string DisplayName => "Magnet Hole";
        public override string WarnMessage => "The hole is acting strange...";
        public override string ActivateMessage => "Your golf ball will now repel from the hole!";
        public override float Weight => Configuration.WeightMagnetHole.Value;
        public override bool IsEnabled => Configuration.EnableMagnetHole.Value;

        public override void OnActivate()
        {
            if (GolfHoleManager.MainHole == null)
            {
                GolfMayhemPlugin.Log.LogWarning("[MagnetHole] No main hole registered yet — event skipped.");
                return;
            }

            GolfMayhemPlugin.Log.LogInfo($"[MagnetHole] Activated — repelling from hole at {GolfHoleManager.MainHole.transform.position}");
        }

        public override void OnDeactivate()
        {
            GolfMayhemPlugin.Log.LogInfo("[MagnetHole] Deactivated.");
        }

        public override void OnUpdate()
        {
            var hole = GolfHoleManager.MainHole;
            if (hole == null) return;

            Vector3 holePos = hole.transform.position;

            var balls = Object.FindObjectsByType<GolfBall>(FindObjectsSortMode.None);
            foreach (var ball in balls)
            {
                if (ball == null || ball.Rigidbody == null) continue;
                if (ball.IsHidden) continue;

                Vector3 dir = ball.transform.position - holePos;
                float dist = dir.magnitude;

                if (dist < REPEL_RADIUS && dist > 0.01f)
                {
                    float strength = REPEL_FORCE * (1f - (dist / REPEL_RADIUS));
                    ball.Rigidbody.AddForce(dir.normalized * strength, ForceMode.Acceleration);
                }
            }
        }
    }
}
