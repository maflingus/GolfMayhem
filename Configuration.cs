using BepInEx.Configuration;

namespace GolfMayhem
{
    /// <summary>
    /// Central configuration for GolfMayhem.
    /// All values are editable in BepInEx/config/com.golfmayhem.superbattlegolf.cfg
    /// and hot-reloadable with the BepInEx Configuration Manager plugin.
    /// </summary>
    public static class Configuration
    {
        // ── Chaos Events ──────────────────────────────────────────────────────
        public static ConfigEntry<bool>  ChaosEventsEnabled      { get; private set; }
        public static ConfigEntry<float> ChaosEventIntervalMin   { get; private set; }
        public static ConfigEntry<float> ChaosEventIntervalMax   { get; private set; }
        public static ConfigEntry<float> ChaosEventDuration      { get; private set; }

        // Per-event toggles (lets server hosts fine-tune chaos)
        public static ConfigEntry<bool>  EnableGravityFlip       { get; private set; }
        public static ConfigEntry<bool>  EnableSpeedSurge        { get; private set; }
        public static ConfigEntry<bool>  EnableMineFlood         { get; private set; }
        public static ConfigEntry<bool>  EnableMagnetHole        { get; private set; }
        public static ConfigEntry<bool>  EnableFogOfWar          { get; private set; }

        // Event weight multipliers — higher = more likely to be chosen
        public static ConfigEntry<float> WeightGravityFlip       { get; private set; }
        public static ConfigEntry<float> WeightSpeedSurge        { get; private set; }
        public static ConfigEntry<float> WeightMineFlood         { get; private set; }
        public static ConfigEntry<float> WeightMagnetHole        { get; private set; }
        public static ConfigEntry<float> WeightFogOfWar          { get; private set; }

        // ── Announcer HUD ─────────────────────────────────────────────────────
        public static ConfigEntry<bool>   AnnouncerEnabled        { get; private set; }
        public static ConfigEntry<float>  AnnouncerFontSize       { get; private set; }
        public static ConfigEntry<float>  AnnouncerDisplayTime    { get; private set; }
        public static ConfigEntry<string> AnnouncerHoleInOneText  { get; private set; }
        public static ConfigEntry<string> AnnouncerFirstToHole    { get; private set; }
        public static ConfigEntry<string> AnnouncerChaosIncoming  { get; private set; }

        // ── Physics Tweaks ────────────────────────────────────────────────────
        public static ConfigEntry<float> GravityFlipMultiplier   { get; private set; }
        public static ConfigEntry<float> SpeedSurgeMultiplier    { get; private set; }

        // ─────────────────────────────────────────────────────────────────────

        public static void Initialize(ConfigFile cfg)
        {
            // ── Chaos Events ──────────────────────────────────────────────────
            ChaosEventsEnabled = cfg.Bind(
                "ChaosEvents", "Enabled", true,
                "Master toggle for the Chaos Event system.");

            ChaosEventIntervalMin = cfg.Bind(
                "ChaosEvents", "IntervalMin", 20f,
                "Minimum seconds between chaos events.");

            ChaosEventIntervalMax = cfg.Bind(
                "ChaosEvents", "IntervalMax", 45f,
                "Maximum seconds between chaos events.");

            ChaosEventDuration = cfg.Bind(
                "ChaosEvents", "Duration", 8f,
                "How many seconds each chaos event lasts.");

            // Per-event toggles
            EnableGravityFlip = cfg.Bind("ChaosEvents.Events", "GravityFlip", true,
                "Briefly inverts or amplifies gravity for all balls.");
            EnableSpeedSurge = cfg.Bind("ChaosEvents.Events", "SpeedSurge", true,
                "Doubles ball velocity for all players.");
            EnableMineFlood = cfg.Bind("ChaosEvents.Events", "MineFlood", true,
                "Spawns a wave of mines across the course.");
            EnableMagnetHole = cfg.Bind("ChaosEvents.Events", "MagnetHole", true,
                "The hole briefly repels ALL balls away from it.");
            EnableFogOfWar = cfg.Bind("ChaosEvents.Events", "FogOfWar", true,
                "Blankets the course in thick fog, hiding player positions.");

            // Event weights
            WeightGravityFlip = cfg.Bind("ChaosEvents.Weights", "GravityFlip", 1.0f, "Relative spawn weight.");
            WeightSpeedSurge  = cfg.Bind("ChaosEvents.Weights", "SpeedSurge",  1.2f, "Relative spawn weight.");
            WeightMineFlood   = cfg.Bind("ChaosEvents.Weights", "MineFlood",   0.8f, "Relative spawn weight.");
            WeightMagnetHole  = cfg.Bind("ChaosEvents.Weights", "MagnetHole",  1.0f, "Relative spawn weight.");
            WeightFogOfWar    = cfg.Bind("ChaosEvents.Weights", "FogOfWar",    1.0f, "Relative spawn weight.");

            // ── Announcer HUD ─────────────────────────────────────────────────
            AnnouncerEnabled = cfg.Bind(
                "AnnouncerHUD", "Enabled", true,
                "Show screen announcements for chaos events and golf moments.");

            AnnouncerFontSize = cfg.Bind(
                "AnnouncerHUD", "FontSize", 52f,
                "Font size for announcement text.");

            AnnouncerDisplayTime = cfg.Bind(
                "AnnouncerHUD", "DisplayTime", 3.5f,
                "Seconds the announcement text stays on screen.");

            AnnouncerHoleInOneText = cfg.Bind(
                "AnnouncerHUD", "HoleInOneText", "⛳ HOLE IN ONE! UNBELIEVABLE!",
                "Text shown on a hole-in-one.");

            AnnouncerFirstToHole = cfg.Bind(
                "AnnouncerHUD", "FirstToHoleText", "🏁 {0} SINKS IT FIRST!",
                "Text shown when someone reaches the hole first. {0} = player name.");

            AnnouncerChaosIncoming = cfg.Bind(
                "AnnouncerHUD", "ChaosIncomingText", "⚠️ CHAOS INCOMING!",
                "Text shown as a chaos event is triggered.");

            // ── Physics ───────────────────────────────────────────────────────
            GravityFlipMultiplier = cfg.Bind(
                "Physics", "GravityFlipMultiplier", -2.5f,
                "Y-gravity during a GravityFlip event. Negative = upward.");

            SpeedSurgeMultiplier = cfg.Bind(
                "Physics", "SpeedSurgeMultiplier", 2.5f,
                "Velocity multiplier applied to all balls during SpeedSurge.");

            GolfMayhemPlugin.Log.LogInfo("Configuration initialized.");
        }
    }
}
