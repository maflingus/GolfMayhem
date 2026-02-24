using System.Collections;
using UnityEngine;

namespace GolfMayhem.ChaosEventSystem.Events
{
    public class TornadoEvent : ChaosEvent
    {
        // Tornado shape parameters
        private const float TOTAL_DURATION = 15f;  // seconds to spiral up
        private const float MAX_HEIGHT = 50f;  // peak height above start
        private const float START_RADIUS = 0.5f; // tight at the bottom
        private const float END_RADIUS = 10f;  // wide at the top
        private const float ROTATIONS = 6f;   // full rotations during ascent
        private const float ANGULAR_SPEED = 360f * ROTATIONS / TOTAL_DURATION; // degrees/sec

        private Coroutine _tornadoCoroutine;

        public override string DisplayName => "Tornado";
        public override string NetworkId => "Tornado";
        public override string WarnMessage => "The wind is picking up...";
        public override string ActivateMessage => "Tornado! Hold on!";
        public override float Weight => Configuration.WeightTornado.Value;
        public override bool IsEnabled => Configuration.EnableTornado.Value;

        public override void OnActivate()
        {
            var local = GameManager.LocalPlayerInfo;
            if (local?.Rigidbody == null) return;

            _tornadoCoroutine = ChaosEventManager.Instance.StartCoroutine(TornadoRoutine(local));
        }

        public override void OnDeactivate()
        {
            if (_tornadoCoroutine != null)
            {
                ChaosEventManager.Instance.StopCoroutine(_tornadoCoroutine);
                _tornadoCoroutine = null;
            }

            // Re-enable gravity so player falls back down naturally
            var local = GameManager.LocalPlayerInfo;
            if (local?.Rigidbody != null)
                local.Rigidbody.useGravity = true;

            GolfMayhemPlugin.Log.LogInfo("[Tornado] Deactivated.");
        }

        private IEnumerator TornadoRoutine(PlayerInfo player)
        {
            var rb = player.Rigidbody;

            // Snapshot the starting ground position
            Vector3 groundOrigin = rb.position;
            float elapsed = 0f;
            float angle = 0f;

            // Disable gravity so we fully control vertical movement
            rb.useGravity = false;

            GolfMayhemPlugin.Log.LogInfo("[Tornado] Spiral started.");

            while (elapsed < TOTAL_DURATION)
            {
                float dt = Time.fixedDeltaTime;
                elapsed += dt;

                float t = Mathf.Clamp01(elapsed / TOTAL_DURATION);

                // Height increases linearly
                float targetHeight = Mathf.Lerp(0f, MAX_HEIGHT, t);

                // Radius expands from tight to wide (inverted cone = starts narrow)
                float radius = Mathf.Lerp(START_RADIUS, END_RADIUS, t);

                // Rotate angle
                angle += ANGULAR_SPEED * dt;

                float rad = angle * Mathf.Deg2Rad;
                Vector3 targetPos = new Vector3(
                    groundOrigin.x + Mathf.Cos(rad) * radius,
                    groundOrigin.y + targetHeight,
                    groundOrigin.z + Mathf.Sin(rad) * radius);

                // Smoothly move rigidbody toward target position each fixed step
                rb.MovePosition(Vector3.Lerp(rb.position, targetPos, 0.25f));

                // Zero out velocity so player input doesn't fight the tornado
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;

                yield return new WaitForFixedUpdate();
            }

            // Re-enable gravity at peak so player falls naturally
            rb.useGravity = true;
            _tornadoCoroutine = null;
            GolfMayhemPlugin.Log.LogInfo("[Tornado] Spiral complete, player falling.");
        }
    }
}