using System.Collections.Generic;
using UnityEngine;

namespace GolfMayhem.ChaosEventSystem.Events
{
    /// <summary>
    /// CHAOS EVENT: Mine Flood
    /// Scatters visual marker mines across the course.
    ///
    /// NETWORKING NOTE: Landmine is a Mirror NetworkBehaviour — only the server
    /// can legitimately spawn it via NetworkServer.Spawn. Since we don't have
    /// a reference to the game's Landmine prefab at load time (it's an Addressable),
    /// we use visual marker objects client-side, plus an explosion impulse via
    /// Rigidbody.AddExplosionForce when triggered.
    ///
    /// TO UPGRADE: If you find the Landmine prefab path (check GameManager.ItemSettings
    /// or ItemCollection in dnSpy), replace SpawnMarkerMine() with a proper
    /// server-side NetworkServer.Spawn call using the real prefab.
    ///
    /// Center point confirmed: GolfHoleManager.MainHole.transform.position
    /// </summary>
    public class MineFloodEvent : ChaosEvent
    {
        private const int   MINE_COUNT        = 12;
        private const float SPAWN_RADIUS      = 30f;
        private const float MINE_HEIGHT_OFFSET = 0.15f;

        private readonly List<GameObject> _spawnedMines = new List<GameObject>();

        public override string DisplayName     => "Mine Flood";
        public override string WarnMessage     => "⚠️ SOMETHING IS BEING PLANTED...";
        public override string ActivateMessage => "💣 MINE FLOOD! WATCH YOUR STEP!";
        public override float  Weight          => Configuration.WeightMineFlood.Value;
        public override bool   IsEnabled       => Configuration.EnableMineFlood.Value;

        public override void OnActivate()
        {
            _spawnedMines.Clear();

            // GolfHoleManager.MainHole confirmed via decompiled source.
            // If null (unlikely mid-round), fall back to world origin.
            Vector3 center = GolfHoleManager.MainHole != null
                ? GolfHoleManager.MainHole.transform.position
                : Vector3.zero;

            for (int i = 0; i < MINE_COUNT; i++)
            {
                Vector3 pos  = GetRandomGroundPosition(center);
                var     mine = SpawnMarkerMine(pos);
                if (mine != null) _spawnedMines.Add(mine);
            }

            GolfMayhemPlugin.Log.LogInfo($"[MineFlood] Spawned {_spawnedMines.Count} mines around {center}.");
        }

        public override void OnDeactivate()
        {
            foreach (var mine in _spawnedMines)
            {
                if (mine != null) Object.Destroy(mine);
            }
            _spawnedMines.Clear();
            GolfMayhemPlugin.Log.LogInfo("[MineFlood] Remaining mines cleaned up.");
        }

        public override void OnUpdate() { }

        // ─────────────────────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────────────────────

        private static GameObject SpawnMarkerMine(Vector3 position)
        {
            // Client-side visual mine with proximity trigger.
            // Uses the real GolfBall class to detect trigger entry — if a GolfBall
            // enters the trigger radius, apply explosive force to its Rigidbody.
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.transform.position   = position;
            go.transform.localScale = Vector3.one * 0.45f;
            go.name                 = "GolfMayhem_MineMaker";

            // Red material to make it visually obvious
            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = new Color(0.8f, 0.1f, 0.1f);
            }

            go.AddComponent<GolfMayhemProximityMine>();
            return go;
        }

        private static Vector3 GetRandomGroundPosition(Vector3 center)
        {
            Vector2 disc      = Random.insideUnitCircle * SPAWN_RADIUS;
            Vector3 candidate = new Vector3(center.x + disc.x, center.y + 20f, center.z + disc.y);

            if (Physics.Raycast(candidate, Vector3.down, out RaycastHit hit, 60f))
            {
                return hit.point + Vector3.up * MINE_HEIGHT_OFFSET;
            }

            return new Vector3(candidate.x, center.y + MINE_HEIGHT_OFFSET, candidate.z);
        }
    }

    /// <summary>
    /// Proximity trigger component attached to GolfMayhem marker mines.
    /// Detects real GolfBall components entering the trigger and applies
    /// explosive physics force to their Rigidbody.
    /// </summary>
    internal class GolfMayhemProximityMine : MonoBehaviour
    {
        private const float TRIGGER_RADIUS  = 1.2f;
        private const float EXPLOSION_FORCE = 900f;
        private const float EXPLOSION_RADIUS = 6f;
        private bool _triggered = false;

        private void Start()
        {
            // Ensure we have a trigger collider — CreatePrimitive adds a non-trigger by default.
            var col = GetComponent<SphereCollider>();
            if (col == null) col = gameObject.AddComponent<SphereCollider>();
            col.radius    = TRIGGER_RADIUS;
            col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_triggered) return;

            // Check for a real GolfBall component — far more reliable than name matching.
            var ball = other.GetComponentInParent<GolfBall>();
            if (ball == null) return;

            _triggered = true;
            Explode(ball);
        }

        private void Explode(GolfBall triggeringBall)
        {
            GolfMayhemPlugin.Log.LogDebug($"[ProximityMine] Exploded at {transform.position} — triggered by {triggeringBall.Owner?.PlayerInfo?.Name ?? "unknown"}");

            // Apply explosive force to all GolfBalls in range
            var nearbyBalls = Object.FindObjectsByType<GolfBall>(FindObjectsSortMode.None);
            foreach (var ball in nearbyBalls)
            {
                if (ball == null || ball.Rigidbody == null || ball.IsHidden) continue;
                ball.Rigidbody.AddExplosionForce(
                    EXPLOSION_FORCE,
                    transform.position,
                    EXPLOSION_RADIUS,
                    upwardsModifier: 1.5f,
                    ForceMode.Impulse
                );
            }

            Object.Destroy(gameObject);
        }
    }
}
