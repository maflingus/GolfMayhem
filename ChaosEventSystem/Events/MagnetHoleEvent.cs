using UnityEngine;

namespace GolfMayhem.ChaosEventSystem.Events
{
    /// <summary>
    /// CHAOS EVENT: Magnet Hole
    /// The hole temporarily repels ALL golf balls — the cup fights back.
    ///
    /// Uses real game classes confirmed from decompiled source:
    ///   - GolfHoleManager.MainHole  (static property, returns GolfHole)
    ///   - GolfBall.Rigidbody        (public property on GolfBall)
    ///   - Rigidbody.linearVelocity  (Unity 6 API)
    /// </summary>
    public class MagnetHoleEvent : ChaosEvent
    {
        private const float REPEL_FORCE  = 35f;
        private const float REPEL_RADIUS = 12f;

        public override string DisplayName     => "Magnet Hole";
        public override string WarnMessage     => "⚠️ THE HOLE IS ACTING STRANGE...";
        public override string ActivateMessage => "🕳️ MAGNET HOLE! THE HOLE IS FIGHTING BACK!";
        public override float  Weight          => Configuration.WeightMagnetHole.Value;
        public override bool   IsEnabled       => Configuration.EnableMagnetHole.Value;

        public override void OnActivate()
        {
            // GolfHoleManager.MainHole is confirmed — it's a static property that
            // returns null if no hole is registered, so we just check for null.
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
            // GolfHoleManager.MainHole can change between holes, so re-fetch each frame.
            var hole = GolfHoleManager.MainHole;
            if (hole == null) return;

            Vector3 holePos = hole.transform.position;

            // Find all real GolfBall instances — no tag guessing needed.
            var balls = Object.FindObjectsByType<GolfBall>(FindObjectsSortMode.None);
            foreach (var ball in balls)
            {
                if (ball == null || ball.Rigidbody == null) continue;
                // Don't apply force to hidden balls (in hole or respawning)
                if (ball.IsHidden) continue;

                Vector3 dir  = ball.transform.position - holePos;
                float   dist = dir.magnitude;

                if (dist < REPEL_RADIUS && dist > 0.01f)
                {
                    // Stronger repulsion closer to the hole
                    float strength = REPEL_FORCE * (1f - (dist / REPEL_RADIUS));
                    ball.Rigidbody.AddForce(dir.normalized * strength, ForceMode.Acceleration);
                }
            }
        }
    }
}
