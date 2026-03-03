using UnityEngine;

namespace GolfMayhem.Patches
{
    public static class GameSessionPatch
    {
        private static bool _subscribed = false;

        public static void Subscribe()
        {
            if (_subscribed) return;
            _subscribed = true;
            PlayerGolfer.AnyPlayerMatchResolutionChanged += OnAnyPlayerMatchResolutionChanged;
            PlayerGolfer.PlayerHitOwnBall += OnPlayerHitOwnBall;
            PlayerGolfer.AnyPlayerEliminated += OnPlayerEliminated;
            GolfMayhemPlugin.Log.LogInfo("[GolfMayhem] Subscribed to game events.");
        }

        public static void Unsubscribe()
        {
            if (!_subscribed) return;
            _subscribed = false;
            PlayerGolfer.AnyPlayerMatchResolutionChanged -= OnAnyPlayerMatchResolutionChanged;
            PlayerGolfer.PlayerHitOwnBall -= OnPlayerHitOwnBall;
            PlayerGolfer.AnyPlayerEliminated -= OnPlayerEliminated;
            GolfMayhemPlugin.Log.LogInfo("[GolfMayhem] Unsubscribed from game events.");
        }

        private static void OnAnyPlayerMatchResolutionChanged(
            PlayerGolfer player,
            PlayerMatchResolution previousResolution,
            PlayerMatchResolution currentResolution)
        {
        }

        private static void OnPlayerEliminated(PlayerGolfer player)
        {
        }

        private static void OnPlayerHitOwnBall(PlayerGolfer player)
        {
            GolfMayhemPlugin.Log.LogDebug($"[GolfMayhem] {player?.PlayerInfo?.Name ?? "?"} hit their ball.");
        }

        public static void OnNewHoleStarted()
        {
            GolfMayhemPlugin.Log.LogDebug("[GolfMayhem] Per-hole state reset for new hole.");
        }
    }
}