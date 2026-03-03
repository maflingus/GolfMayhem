using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Mirror;
using UnityEngine;
using GolfMayhem.ChaosEventSystem;
using GolfMayhem.Patches;

namespace GolfMayhem
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class GolfMayhemPlugin : BaseUnityPlugin
    {
        public static GolfMayhemPlugin Instance { get; private set; }
        internal static new ManualLogSource Log { get; private set; }

        private Harmony _harmony;
        private static bool _systemsInjected = false;

        private void Awake()
        {
            if (Instance != null) { Destroy(this); return; }
            Instance = this;

            Log = base.Logger;
            Log.LogInfo($"GolfMayhem {PluginInfo.PLUGIN_VERSION} loading...");

            Configuration.Initialize(Config);
            Patches.RulesPatch.InitFromConfig();

            _harmony = new Harmony(PluginInfo.PLUGIN_GUID);
            _harmony.PatchAll(typeof(GolfMayhemPlugin).Assembly);
            Log.LogInfo("Harmony patches applied.");

            GameSessionPatch.Subscribe();
            CourseManager.MatchStateChanged += OnMatchStateChanged;
            GolfMayhemNetwork.Initialize();

            Log.LogInfo("GolfMayhem by Maflingus loaded. Let chaos reign.");
        }

        private void OnDestroy()
        {
            CourseManager.MatchStateChanged -= OnMatchStateChanged;
            GameSessionPatch.Unsubscribe();
            GolfMayhemNetwork.Shutdown();
            _harmony?.UnpatchSelf();
            Log.LogInfo("GolfMayhem unloaded.");
        }

        private void OnMatchStateChanged(MatchState previousState, MatchState currentState)
        {
            Log.LogDebug($"[MatchState] {previousState} → {currentState}");

            switch (currentState)
            {
                case MatchState.TeeOff:
                    // Spawn systems early so they exist before Ongoing
                    if (!_systemsInjected && !SingletonBehaviour<DrivingRangeManager>.HasInstance)
                    {
                        SpawnLocalSystems();
                        _systemsInjected = true;
                    }

                    // Host broadcasts environment settings to all clients
                    if (NetworkServer.active && EnvironmentManager.Instance != null)
                    {
                        bool rain = RulesPatch.RainEnabled;
                        bool nightTime = RulesPatch.NightTimeEnabled;
                        EnvironmentManager.Instance.BroadcastEnvironment(rain, nightTime);
                    }
                    break;

                case MatchState.Ongoing:
                    GameSessionPatch.OnNewHoleStarted();
                    break;

                case MatchState.Ended:
                    _systemsInjected = false;
                    CleanupGameSystems();
                    break;
            }
        }

        public void SpawnLocalSystems()
        {
            if (Configuration.ChaosEventsEnabled.Value && ChaosEventManager.Instance == null)
            {
                var chaosGO = new GameObject("GolfMayhem_ChaosManager");
                DontDestroyOnLoad(chaosGO);
                chaosGO.AddComponent<ChaosEventManager>();
                Log.LogInfo("ChaosEventManager spawned.");
            }

            if (EnvironmentManager.Instance == null)
            {
                var envGO = new GameObject("GolfMayhem_EnvironmentManager");
                DontDestroyOnLoad(envGO);
                envGO.AddComponent<EnvironmentManager>();
                Log.LogInfo("EnvironmentManager spawned.");
            }
        }

        private static void CleanupGameSystems()
        {
            if (ChaosEventManager.Instance != null)
                Destroy(ChaosEventManager.Instance.gameObject);

            if (EnvironmentManager.Instance != null)
                Destroy(EnvironmentManager.Instance.gameObject);

            Log.LogDebug("Game systems cleaned up.");
        }
    }
}