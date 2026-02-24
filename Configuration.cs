using BepInEx.Configuration;

namespace GolfMayhem
{
    public static class Configuration
    {
        // ── Chaos Events ──────────────────────────────────────────────────────
        public static ConfigEntry<bool> ChaosEventsEnabled { get; private set; }
        public static ConfigEntry<float> ChaosEventIntervalMin { get; private set; }
        public static ConfigEntry<float> ChaosEventIntervalMax { get; private set; }
        public static ConfigEntry<float> ChaosEventDuration { get; private set; }

        // Per-event toggles
        public static ConfigEntry<bool> EnableGravityFlip { get; private set; }
        public static ConfigEntry<bool> EnableSpeedSurge { get; private set; }
        public static ConfigEntry<bool> EnableMineFlood { get; private set; }
        public static ConfigEntry<bool> EnableMagnetHole { get; private set; }
        public static ConfigEntry<bool> EnableFogOfWar { get; private set; }
        public static ConfigEntry<bool> EnableMiniature { get; private set; }
        public static ConfigEntry<bool> EnableOrbitalStrike { get; private set; }
        public static ConfigEntry<bool> EnableGiantMode { get; private set; }
        public static ConfigEntry<bool> EnableNightTime { get; private set; }
        public static ConfigEntry<bool> EnableGolfCartChaos { get; private set; }
        public static ConfigEntry<bool> EnableCoffeeRush { get; private set; }
        public static ConfigEntry<bool> EnableTornado { get; private set; }

        // Event weight multipliers
        public static ConfigEntry<float> WeightGravityFlip { get; private set; }
        public static ConfigEntry<float> WeightSpeedSurge { get; private set; }
        public static ConfigEntry<float> WeightMineFlood { get; private set; }
        public static ConfigEntry<float> WeightMagnetHole { get; private set; }
        public static ConfigEntry<float> WeightFogOfWar { get; private set; }
        public static ConfigEntry<float> WeightMiniature { get; private set; }
        public static ConfigEntry<float> WeightOrbitalStrike { get; private set; }
        public static ConfigEntry<float> WeightGiantMode { get; private set; }
        public static ConfigEntry<float> WeightNightTime { get; private set; }
        public static ConfigEntry<float> WeightGolfCartChaos { get; private set; }
        public static ConfigEntry<float> WeightCoffeeRush { get; private set; }
        public static ConfigEntry<float> WeightTornado { get; private set; }

        // ── Physics Tweaks ────────────────────────────────────────────────────
        public static ConfigEntry<float> GravityFlipMultiplier { get; private set; }
        public static ConfigEntry<float> SpeedSurgeMultiplier { get; private set; }

        public static void Initialize(ConfigFile cfg)
        {
            ChaosEventsEnabled = cfg.Bind("ChaosEvents", "Enabled", true,
                "Master toggle for the Chaos Event system.");
            ChaosEventIntervalMin = cfg.Bind("ChaosEvents", "IntervalMin", 5f,
                "Minimum seconds between chaos events.");
            ChaosEventIntervalMax = cfg.Bind("ChaosEvents", "IntervalMax", 25f,
                "Maximum seconds between chaos events.");
            ChaosEventDuration = cfg.Bind("ChaosEvents", "Duration", 18f,
                "How many seconds each chaos event lasts.");

            EnableGravityFlip = cfg.Bind("ChaosEvents.Events", "GravityFlip", true, "Briefly inverts or amplifies gravity for all balls.");
            EnableSpeedSurge = cfg.Bind("ChaosEvents.Events", "SpeedSurge", false, "Doubles ball velocity for all players.");
            EnableMineFlood = cfg.Bind("ChaosEvents.Events", "MineFlood", true, "Spawns a wave of mines across the course.");
            EnableMagnetHole = cfg.Bind("ChaosEvents.Events", "MagnetHole", true, "The hole briefly repels ALL balls away from it.");
            EnableFogOfWar = cfg.Bind("ChaosEvents.Events", "FogOfWar", true, "Blankets the course in thick fog, hiding player positions.");
            EnableMiniature = cfg.Bind("ChaosEvents.Events", "Miniature", true, "Shrinks every player and ball to half size.");
            EnableOrbitalStrike = cfg.Bind("ChaosEvents.Events", "OrbitalStrike", true, "Fires an orbital laser at every player simultaneously.");
            EnableGiantMode = cfg.Bind("ChaosEvents.Events", "GiantMode", true, "Grows every player and ball to 2.5x size.");
            EnableNightTime = cfg.Bind("ChaosEvents.Events", "NightTime", true, "Dims all lights and fades the scene to near darkness.");
            EnableGolfCartChaos = cfg.Bind("ChaosEvents.Events", "GolfCartChaos", true, "Spawns a golf cart for every player simultaneously.");
            EnableCoffeeRush = cfg.Bind("ChaosEvents.Events", "CoffeeRush", true, "Applies the coffee speed boost to every player.");
            EnableTornado = cfg.Bind("ChaosEvents.Events", "Tornado", true, "Spirals every player upward in a tornado funnel.");

            WeightGravityFlip = cfg.Bind("ChaosEvents.Weights", "GravityFlip", 1.0f, "Relative spawn weight.");
            WeightSpeedSurge = cfg.Bind("ChaosEvents.Weights", "SpeedSurge", 1.0f, "Relative spawn weight.");
            WeightMineFlood = cfg.Bind("ChaosEvents.Weights", "MineFlood", 1.0f, "Relative spawn weight.");
            WeightMagnetHole = cfg.Bind("ChaosEvents.Weights", "MagnetHole", 1.0f, "Relative spawn weight.");
            WeightFogOfWar = cfg.Bind("ChaosEvents.Weights", "FogOfWar", 1.0f, "Relative spawn weight.");
            WeightMiniature = cfg.Bind("ChaosEvents.Weights", "Miniature", 1.0f, "Relative spawn weight.");
            WeightOrbitalStrike = cfg.Bind("ChaosEvents.Weights", "OrbitalStrike", 1.0f, "Relative spawn weight.");
            WeightGiantMode = cfg.Bind("ChaosEvents.Weights", "GiantMode", 1.0f, "Relative spawn weight.");
            WeightNightTime = cfg.Bind("ChaosEvents.Weights", "NightTime", 1.0f, "Relative spawn weight.");
            WeightGolfCartChaos = cfg.Bind("ChaosEvents.Weights", "GolfCartChaos", 1.0f, "Relative spawn weight.");
            WeightCoffeeRush = cfg.Bind("ChaosEvents.Weights", "CoffeeRush", 1.0f, "Relative spawn weight.");
            WeightTornado = cfg.Bind("ChaosEvents.Weights", "Tornado", 1.0f, "Relative spawn weight.");

            GravityFlipMultiplier = cfg.Bind("Physics", "GravityFlipMultiplier", -2.5f,
                "Y-gravity during a GravityFlip event. Negative = upward.");
            SpeedSurgeMultiplier = cfg.Bind("Physics", "SpeedSurgeMultiplier", 2.5f,
                "Velocity multiplier applied to all balls during SpeedSurge.");

            GolfMayhemPlugin.Log.LogInfo("Configuration initialized.");
        }
    }
}