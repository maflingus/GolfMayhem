using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using GolfMayhem.ChaosEventSystem;
using GolfMayhem.HUD;
using GolfMayhem.Patches;

namespace GolfMayhem
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class GolfMayhemPlugin : BaseUnityPlugin
    {
        public static GolfMayhemPlugin Instance { get; private set; }
        internal static new ManualLogSource Log { get; private set; }

        private Harmony _harmony;

        // Track whether game systems have been injected this session
        private static bool _systemsInjected = false;

        private void Awake()
        {
            if (Instance != null) { Destroy(this); return; }
            Instance = this;

            Log = base.Logger;
            Log.LogInfo($"GolfMayhem {PluginInfo.PLUGIN_VERSION} loading...");

            Configuration.Initialize(Config);

            _harmony = new Harmony(PluginInfo.PLUGIN_GUID);
            _harmony.PatchAll(typeof(GolfMayhemPlugin).Assembly);
            Log.LogInfo("Harmony patches applied.");

            // Subscribe to game events immediately — no scene detection needed.
            // These are static C# events on Mirror NetworkBehaviours; they fire
            // whenever the game state changes, regardless of scene name.
            GameEventHooks.Subscribe();

            // CourseManager.MatchStateChanged fires for every state transition.
            // Signature confirmed from source: Action<MatchState, MatchState>
            CourseManager.MatchStateChanged += OnMatchStateChanged;

            Log.LogInfo("GolfMayhem loaded. Let chaos reign. ⛳");
        }

        private void OnDestroy()
        {
            CourseManager.MatchStateChanged -= OnMatchStateChanged;
            GameEventHooks.Unsubscribe();
            _harmony?.UnpatchSelf();
            Log.LogInfo("GolfMayhem unloaded.");
        }

        /// <summary>
        /// Central state machine for GolfMayhem lifecycle.
        ///
        /// MatchState order: Initializing → HoleOverview → TeeOff → Ongoing
        ///                   → CountingDownToEnd → Overtime → Ended
        ///
        /// We gate on Ongoing (not TeeOff) because:
        ///   - TeeOff is the countdown phase — balls aren't live yet
        ///   - Ongoing is when players can actually shoot and chaos is meaningful
        /// </summary>
        private void OnMatchStateChanged(MatchState previousState, MatchState currentState)
        {
            Log.LogDebug($"[MatchState] {previousState} → {currentState}");

            switch (currentState)
            {
                case MatchState.Ongoing:
                    // Hole is live — reset per-hole tracking and start/continue chaos
                    GameEventHooks.OnNewHoleStarted();

                    // Inject game systems the first time a hole goes live this session
                    if (!_systemsInjected)
                    {
                        InjectGameSystems();
                        _systemsInjected = true;
                    }
                    break;

                case MatchState.Ended:
                    // Match is completely over — tear down for this session
                    _systemsInjected = false;
                    CleanupGameSystems();
                    break;
            }
        }

        private void InjectGameSystems()
        {
            if (Configuration.ChaosEventsEnabled.Value)
            {
                var chaosGO = new GameObject("GolfMayhem_ChaosManager");
                DontDestroyOnLoad(chaosGO);
                chaosGO.AddComponent<ChaosEventManager>();
                Log.LogInfo("ChaosEventManager spawned.");
            }

            if (Configuration.AnnouncerEnabled.Value && AnnouncerHUD.Instance == null)
            {
                var hudGO = new GameObject("GolfMayhem_AnnouncerHUD");
                DontDestroyOnLoad(hudGO);
                hudGO.AddComponent<AnnouncerHUD>();
                Log.LogInfo("AnnouncerHUD spawned.");
            }
        }

        private static void CleanupGameSystems()
        {
            if (ChaosEventManager.Instance != null)
            {
                Destroy(ChaosEventManager.Instance.gameObject);
                Log.LogDebug("ChaosEventManager destroyed.");
            }
        }
    }
}
