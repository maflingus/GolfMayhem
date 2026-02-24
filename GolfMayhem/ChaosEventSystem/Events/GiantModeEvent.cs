using System.Collections.Generic;
using UnityEngine;

namespace GolfMayhem.ChaosEventSystem.Events
{
    public class GiantModeEvent : ChaosEvent
    {
        private const float GIANT_SCALE = 2.5f;

        private readonly Dictionary<Transform, Vector3> _originalScales = new Dictionary<Transform, Vector3>();
        private readonly Dictionary<Transform, Vector3> _originalPositions = new Dictionary<Transform, Vector3>();

        public override string DisplayName => "Giant Mode";
        public override string NetworkId => "GiantMode";
        public override string WarnMessage => "Something feels different...";
        public override string ActivateMessage => "Everyone is now giant!";
        public override float Weight => Configuration.WeightGiantMode.Value;
        public override bool IsEnabled => Configuration.EnableGiantMode.Value;

        public override void OnActivate()
        {
            _originalScales.Clear();
            _originalPositions.Clear();

            var players = Object.FindObjectsByType<PlayerInfo>(FindObjectsSortMode.None);
            foreach (var player in players)
            {
                if (player == null || player.HipBone == null) continue;

                var hip = player.HipBone;
                _originalScales[hip] = hip.localScale;
                _originalPositions[hip] = hip.localPosition;

                hip.localScale = Vector3.one * GIANT_SCALE;

                float lift = hip.localPosition.y * (GIANT_SCALE - 1f);
                hip.localPosition += Vector3.up * lift;
            }

            var balls = Object.FindObjectsByType<GolfBall>(FindObjectsSortMode.None);
            foreach (var ball in balls)
            {
                if (ball == null) continue;
                _originalScales[ball.transform] = ball.transform.localScale;
                ball.transform.localScale = Vector3.one * GIANT_SCALE;
            }

            GolfMayhemPlugin.Log.LogInfo($"[GiantMode] Grew {players.Length} players and {balls.Length} balls.");
        }

        public override void OnDeactivate()
        {
            foreach (var kvp in _originalScales)
                if (kvp.Key != null) kvp.Key.localScale = kvp.Value;
            foreach (var kvp in _originalPositions)
                if (kvp.Key != null) kvp.Key.localPosition = kvp.Value;
            _originalScales.Clear();
            _originalPositions.Clear();
            GolfMayhemPlugin.Log.LogInfo("[GiantMode] Restored original scales.");
        }
    }
}