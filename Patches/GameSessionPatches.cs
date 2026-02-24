using UnityEngine;
using GolfMayhem.HUD;

namespace GolfMayhem.Patches
{
    /// <summary>
    /// Hooks into the real game event system using static C# events confirmed
    /// from the decompiled Assembly-CSharp source. No Harmony patches needed
    /// for these — the game already broadcasts the events we want!
    ///
    /// Confirmed events from PlayerGolfer.cs:
    ///   - PlayerGolfer.PlayerHitOwnBall         (Action<PlayerGolfer>)
    ///   - PlayerGolfer.AnyPlayerEliminated       (Action<PlayerGolfer>)
    ///   - PlayerGolfer.AnyPlayerMatchResolutionChanged (Action<PlayerGolfer, PlayerMatchResolution, PlayerMatchResolution>)
    ///   - PlayerGolfer.LocalPlayerMatchResolutionChanged (Action<PlayerMatchResolution, PlayerMatchResolution>)
    ///
    /// Confirmed from CourseManager usage in GolfBall.cs:
    ///   - CourseManager.MatchStateChanged (seen as event in GolfBall.OnStartServer)
    ///
    /// Player name access: playerGolfer.PlayerInfo.Name (via PlayerInfo component)
    /// Match resolution enum values: Scored, Eliminated, JoinedAsSpectator, None, Uninitialized
    /// </summary>
    public static class GameEventHooks
    {
        private static bool _subscribed = false;

        // Tracks the first player to score each hole for "First to Hole" announcement
        private static bool _firstScoredThisHole = false;

        /// <summary>
        /// Call this when a gameplay scene loads to start receiving events.
        /// </summary>
        public static void Subscribe()
        {
            if (_subscribed) return;
            _subscribed = true;
            _firstScoredThisHole = false;

            // Fires whenever any player sinks their ball (Scored) or gets eliminated
            PlayerGolfer.AnyPlayerMatchResolutionChanged += OnAnyPlayerMatchResolutionChanged;

            // Fires whenever a player successfully hits their own golf ball
            PlayerGolfer.PlayerHitOwnBall += OnPlayerHitOwnBall;

            // Fires when a player's resolution becomes Eliminated specifically
            PlayerGolfer.AnyPlayerEliminated += OnPlayerEliminated;

            GolfMayhemPlugin.Log.LogInfo("[GameEventHooks] Subscribed to game events.");
        }

        /// <summary>
        /// Call this when the gameplay scene unloads to clean up.
        /// </summary>
        public static void Unsubscribe()
        {
            if (!_subscribed) return;
            _subscribed = false;

            PlayerGolfer.AnyPlayerMatchResolutionChanged -= OnAnyPlayerMatchResolutionChanged;
            PlayerGolfer.PlayerHitOwnBall                -= OnPlayerHitOwnBall;
            PlayerGolfer.AnyPlayerEliminated             -= OnPlayerEliminated;

            GolfMayhemPlugin.Log.LogInfo("[GameEventHooks] Unsubscribed from game events.");
        }

        // ─────────────────────────────────────────────────────────────
        // Event Handlers
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Called when any player's match resolution changes.
        /// PlayerMatchResolution values: Uninitialized, None, Scored, Eliminated, JoinedAsSpectator
        /// </summary>
        private static void OnAnyPlayerMatchResolutionChanged(
            PlayerGolfer player,
            PlayerMatchResolution previousResolution,
            PlayerMatchResolution currentResolution)
        {
            if (!Configuration.AnnouncerEnabled.Value) return;

            // PlayerGolfer.PlayerInfo.Name confirmed from GolfBall.cs:
            //   nameTag.SetName(string.Format(..., Networkowner.PlayerInfo.Name))
            string playerName = player?.PlayerInfo?.Name ?? "Someone";

            if (currentResolution == PlayerMatchResolution.Scored)
            {
                if (!_firstScoredThisHole)
                {
                    // First player to hole out this round
                    _firstScoredThisHole = true;
                    string msg = string.Format(Configuration.AnnouncerFirstToHole.Value, playerName);
                    AnnouncerHUD.Instance?.ShowAnnouncement(msg, new Color(1f, 0.85f, 0f)); // Gold
                    GolfMayhemPlugin.Log.LogInfo($"[GameEvents] First to hole: {playerName}");
                }
                else
                {
                    // Subsequent score
                    string msg = $"🏌️ {playerName} SINKS IT!";
                    AnnouncerHUD.Instance?.ShowAnnouncement(msg, Color.white);
                }
            }
        }

        /// <summary>
        /// Called when any player is eliminated (falls off, timeout, orbital laser, etc.)
        /// AnyPlayerEliminated fires specifically for the Eliminated resolution,
        /// so we don't need to re-check the resolution here.
        /// </summary>
        private static void OnPlayerEliminated(PlayerGolfer player)
        {
            if (!Configuration.AnnouncerEnabled.Value) return;

            string playerName = player?.PlayerInfo?.Name ?? "Someone";
            string msg        = $"💀 {playerName} IS OUT!";
            AnnouncerHUD.Instance?.ShowAnnouncement(msg, new Color(1f, 0.3f, 0.3f)); // Red
            GolfMayhemPlugin.Log.LogInfo($"[GameEvents] Player eliminated: {playerName}");
        }

        /// <summary>
        /// Called every time any player successfully hits their own ball.
        /// Good hook for shot-based announcements (e.g. chaos event reactions).
        /// </summary>
        private static void OnPlayerHitOwnBall(PlayerGolfer player)
        {
            // Currently just logging — wire AnnouncerHUD here if you want
            // per-shot announcements (e.g. "NICE SHOT!" on perfect swings).
            GolfMayhemPlugin.Log.LogDebug($"[GameEvents] {player?.PlayerInfo?.Name ?? "?"} hit their ball.");
        }

        /// <summary>
        /// Reset per-hole tracking when a new hole begins.
        /// Call this from Plugin.cs if you hook CourseManager.MatchStateChanged.
        /// </summary>
        public static void OnNewHoleStarted()
        {
            _firstScoredThisHole = false;
            GolfMayhemPlugin.Log.LogDebug("[GameEventHooks] Per-hole state reset for new hole.");
        }
    }
}

