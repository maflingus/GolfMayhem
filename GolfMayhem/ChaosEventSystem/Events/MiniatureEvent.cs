using System.Collections.Generic;
using UnityEngine;

namespace GolfMayhem.ChaosEventSystem.Events
{

    public class MiniatureEvent : ChaosEvent
    {
        private const float SMALL_SCALE = 0.4f;

        private readonly Dictionary<Transform, Vector3> _originalScales = new Dictionary<Transform, Vector3>();

        public override string DisplayName     => "Miniature Mode";
        public override string NetworkId       => "MiniatureMode";
        public override string WarnMessage     => "Something feels different...";
        public override string ActivateMessage => "Everyone is now tiny!";
        public override float  Weight          => Configuration.WeightMiniature.Value;
        public override bool   IsEnabled       => Configuration.EnableMiniature.Value;

        public override void OnActivate()
        {
            _originalScales.Clear();

            var players = Object.FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);
            foreach (var player in players)
            {
                if (player == null) continue;
                _originalScales[player.transform] = player.transform.localScale;
                player.transform.localScale = Vector3.one * SMALL_SCALE;
            }

            var balls = Object.FindObjectsByType<GolfBall>(FindObjectsSortMode.None);
            foreach (var ball in balls)
            {
                if (ball == null) continue;
                _originalScales[ball.transform] = ball.transform.localScale;
                ball.transform.localScale = Vector3.one * SMALL_SCALE;
            }

            GolfMayhemPlugin.Log.LogInfo($"[Miniature] Shrunk {players.Length} players and {balls.Length} balls.");
        }

        public override void OnDeactivate()
        {
            foreach (var kvp in _originalScales)
            {
                if (kvp.Key != null)
                    kvp.Key.localScale = kvp.Value;
            }
            _originalScales.Clear();
            GolfMayhemPlugin.Log.LogInfo("[Miniature] Restored original scales.");
        }
    }
}
