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

            _harmony = new Harmony(PluginInfo.PLUGIN_GUID);
            _harmony.PatchAll(typeof(GolfMayhemPlugin).Assembly);
            Log.LogInfo("Harmony patches applied.");

            GameEventHooks.Subscribe();
            CourseManager.MatchStateChanged += OnMatchStateChanged;
            GolfMayhemNetwork.Initialize();

            Log.LogInfo("GolfMayhem loaded. Let chaos reign...");
        }

        private void OnDestroy()
        {
            CourseManager.MatchStateChanged -= OnMatchStateChanged;
            GameEventHooks.Unsubscribe();
            GolfMayhemNetwork.Shutdown();
            _harmony?.UnpatchSelf();
            Log.LogInfo("GolfMayhem unloaded.");
        }

        private void OnMatchStateChanged(MatchState previousState, MatchState currentState)
        {
            Log.LogDebug($"[MatchState] {previousState} → {currentState}");

            switch (currentState)
            {
                case MatchState.Ongoing:
                    GameEventHooks.OnNewHoleStarted();
                    if (!_systemsInjected)
                    {
                        SpawnLocalSystems();
                        _systemsInjected = true;
                    }
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
        }

        private static void CleanupGameSystems()
        {
            if (ChaosEventManager.Instance != null)
                Destroy(ChaosEventManager.Instance.gameObject);

            Log.LogDebug("Game systems cleaned up.");
        }
    }
}