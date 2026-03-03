using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GolfMayhem.Patches
{

    public static class RulesPatch
    {
        public static bool RainEnabled { get; set; } = false;
        public static bool NightTimeEnabled { get; set; } = false;
        public static bool DisableCoffeeRush { get; set; } = false;
        public static bool DisableFogOfWar { get; set; } = false;
        public static bool DisableGauntlet { get; set; } = false;
        public static bool DisableGiantMode { get; set; } = false;
        public static bool DisableGolfCartChaos { get; set; } = false;
        public static bool DisableGolfCartRace { get; set; } = false;
        public static bool DisableGravityFlip { get; set; } = false;
        public static bool DisableMagnetHole { get; set; } = false;
        public static bool DisableMineFlood { get; set; } = false;
        public static bool DisableMiniature { get; set; } = false;
        public static bool DisableNightTime { get; set; } = false;
        public static bool DisableOrbitalStrike { get; set; } = false;
        public static bool DisableRain { get; set; } = false;
        public static bool DisableSpeedSurge { get; set; } = false;
        public static bool DisableTornado { get; set; } = false;

        public static void InitFromConfig()
        {
            DisableCoffeeRush = !Configuration.EnableCoffeeRush.Value;
            DisableFogOfWar = !Configuration.EnableFogOfWar.Value;
            DisableGauntlet = !Configuration.EnableGauntlet.Value;
            DisableGiantMode = !Configuration.EnableGiantMode.Value;
            DisableGolfCartChaos = !Configuration.EnableGolfCartChaos.Value;
            DisableGolfCartRace = !Configuration.EnableGolfCartRace.Value;
            DisableGravityFlip = !Configuration.EnableGravityFlip.Value;
            DisableMagnetHole = !Configuration.EnableMagnetHole.Value;
            DisableMineFlood = !Configuration.EnableMineFlood.Value;
            DisableMiniature = !Configuration.EnableMiniature.Value;
            DisableNightTime = !Configuration.EnableNightTime.Value;
            DisableOrbitalStrike = !Configuration.EnableOrbitalStrike.Value;
            DisableRain = !Configuration.EnableRain.Value;
            DisableSpeedSurge = !Configuration.EnableSpeedSurge.Value;
            DisableTornado = !Configuration.EnableTornado.Value;
        }
    }

    [HarmonyPatch(typeof(MatchSetupRules), "Initialize")]
    public static class Patch_MatchSetupRules_Initialize
    {
        [HarmonyPostfix]
        public static void Postfix(MatchSetupRules __instance)
        {
            try
            {
                InjectOptions(__instance);
            }
            catch (System.Exception ex)
            {
                GolfMayhemPlugin.Log.LogError($"[GolfMayhem] Failed to inject UI: {ex}");
            }
        }

        private static void InjectOptions(MatchSetupRules rules)
        {
            var template = rules.hitOtherPlayersBalls;
            if (template == null)
            {
                GolfMayhemPlugin.Log.LogWarning("[GolfMayhem] Could not find hitOtherPlayersBalls template.");
                return;
            }

            var battleRules = template.transform.parent.parent;
            if (battleRules == null) return;

            var rulesPage = battleRules.parent;
            if (rulesPage == null) return;

            bool isHost = Mirror.NetworkServer.active;

            int battleRulesIndex = -1;
            Transform battleHeader = null;
            for (int i = 0; i < rulesPage.childCount; i++)
            {
                if (rulesPage.GetChild(i).name == "Battle Rules")
                {
                    battleRulesIndex = i;
                    if (i > 0) battleHeader = rulesPage.GetChild(i - 1);
                    break;
                }
            }

            if (battleHeader == null || battleRulesIndex < 0)
            {
                GolfMayhemPlugin.Log.LogWarning("[GolfMayhem] Could not find Battle section.");
                return;
            }

            // ── Environment section ────────────────────────────────────────────
            int insertAt = battleRulesIndex + 1;

            var envHeader = MakeHeader(battleHeader.gameObject, rulesPage, "Environment");
            var envRules = MakeContainer(battleRules.gameObject, rulesPage, "Environment Rules");
            envHeader.transform.SetSiblingIndex(insertAt++);
            envRules.transform.SetSiblingIndex(insertAt++);

            var envParent = envRules.transform.GetChild(0);
            ClearChildren(envParent);

            AddToggle(envParent, template.gameObject, "GolfMayhem_Rain", "Rain", isHost,
                RulesPatch.RainEnabled,
                v => RulesPatch.RainEnabled = v, requireHost: true);

            AddToggle(envParent, template.gameObject, "GolfMayhem_NightTime", "Night time", isHost,
                RulesPatch.NightTimeEnabled,
                v => RulesPatch.NightTimeEnabled = v, requireHost: true);

            // ── Events section ─────────────────────────────────────────────────
            var evtHeader = MakeHeader(battleHeader.gameObject, rulesPage, "Events");
            var evtRules = MakeContainer(battleRules.gameObject, rulesPage, "Event Rules");
            evtHeader.transform.SetSiblingIndex(insertAt++);
            evtRules.transform.SetSiblingIndex(insertAt++);

            var evtParent = evtRules.transform.GetChild(0);
            ClearChildren(evtParent);

            AddToggle(evtParent, template.gameObject, "GolfMayhem_CoffeeRush", "Coffee Rush", isHost, !RulesPatch.DisableCoffeeRush, v => { RulesPatch.DisableCoffeeRush = !v; Configuration.EnableCoffeeRush.Value = v; });
            AddToggle(evtParent, template.gameObject, "GolfMayhem_FogOfWar", "Fog of War", isHost, !RulesPatch.DisableFogOfWar, v => { RulesPatch.DisableFogOfWar = !v; Configuration.EnableFogOfWar.Value = v; });
            AddToggle(evtParent, template.gameObject, "GolfMayhem_Gauntlet", "Gauntlet", isHost, !RulesPatch.DisableGauntlet, v => { RulesPatch.DisableGauntlet = !v; Configuration.EnableGauntlet.Value = v; });
            AddToggle(evtParent, template.gameObject, "GolfMayhem_GiantMode", "Giant Mode", isHost, !RulesPatch.DisableGiantMode, v => { RulesPatch.DisableGiantMode = !v; Configuration.EnableGiantMode.Value = v; });
            AddToggle(evtParent, template.gameObject, "GolfMayhem_GolfCartChaos", "Golf Cart Chaos", isHost, !RulesPatch.DisableGolfCartChaos, v => { RulesPatch.DisableGolfCartChaos = !v; Configuration.EnableGolfCartChaos.Value = v; });
            AddToggle(evtParent, template.gameObject, "GolfMayhem_GolfCartRace", "Golf Cart Race", isHost, !RulesPatch.DisableGolfCartRace, v => { RulesPatch.DisableGolfCartRace = !v; Configuration.EnableGolfCartRace.Value = v; });
            AddToggle(evtParent, template.gameObject, "GolfMayhem_GravityFlip", "Gravity Flip", isHost, !RulesPatch.DisableGravityFlip, v => { RulesPatch.DisableGravityFlip = !v; Configuration.EnableGravityFlip.Value = v; });
            AddToggle(evtParent, template.gameObject, "GolfMayhem_MagnetHole", "Magnet Hole", isHost, !RulesPatch.DisableMagnetHole, v => { RulesPatch.DisableMagnetHole = !v; Configuration.EnableMagnetHole.Value = v; });
            AddToggle(evtParent, template.gameObject, "GolfMayhem_MineFlood", "Mine Flood", isHost, !RulesPatch.DisableMineFlood, v => { RulesPatch.DisableMineFlood = !v; Configuration.EnableMineFlood.Value = v; });
            AddToggle(evtParent, template.gameObject, "GolfMayhem_Miniature", "Miniature", isHost, !RulesPatch.DisableMiniature, v => { RulesPatch.DisableMiniature = !v; Configuration.EnableMiniature.Value = v; });
            AddToggle(evtParent, template.gameObject, "GolfMayhem_NightTimeEvt", "Night Time", isHost, !RulesPatch.DisableNightTime, v => { RulesPatch.DisableNightTime = !v; Configuration.EnableNightTime.Value = v; });
            AddToggle(evtParent, template.gameObject, "GolfMayhem_OrbitalStrike", "Orbital Strike", isHost, !RulesPatch.DisableOrbitalStrike, v => { RulesPatch.DisableOrbitalStrike = !v; Configuration.EnableOrbitalStrike.Value = v; });
            AddToggle(evtParent, template.gameObject, "GolfMayhem_Rain_Evt", "Rain", isHost, !RulesPatch.DisableRain, v => { RulesPatch.DisableRain = !v; Configuration.EnableRain.Value = v; });
            AddToggle(evtParent, template.gameObject, "GolfMayhem_SpeedSurge", "Speed Surge", isHost, !RulesPatch.DisableSpeedSurge, v => { RulesPatch.DisableSpeedSurge = !v; Configuration.EnableSpeedSurge.Value = v; });
            AddToggle(evtParent, template.gameObject, "GolfMayhem_Tornado", "Tornado", isHost, !RulesPatch.DisableTornado, v => { RulesPatch.DisableTornado = !v; Configuration.EnableTornado.Value = v; });

            GolfMayhemPlugin.Log.LogInfo("[GolfMayhem] Environment and Events sections injected.");
        }

        private static GameObject MakeHeader(GameObject source, Transform parent, string text)
        {
            var go = Object.Instantiate(source, parent);
            go.name = text;
            foreach (var comp in go.GetComponents<Component>())
                if (!(comp is Transform || comp is TMP_Text || comp is CanvasRenderer))
                    Object.Destroy(comp);
            var tmp = go.GetComponent<TMP_Text>();
            if (tmp != null) tmp.text = text;
            return go;
        }

        private static GameObject MakeContainer(GameObject source, Transform parent, string name)
        {
            var go = Object.Instantiate(source, parent);
            go.name = name;
            return go;
        }

        private static void ClearChildren(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--)
                Object.Destroy(t.GetChild(i).gameObject);
        }

        private static void AddToggle(Transform parent, GameObject template, string goName, string label,
            bool isHost, bool initialOn, System.Action<bool> onChanged, bool requireHost = false)
        {
            var go = Object.Instantiate(template, parent);
            go.name = goName;
            var dropdown = go.GetComponent<DropdownOption>();
            if (dropdown != null)
            {
                dropdown.Initialize(() =>
                {
                    if (requireHost && !Mirror.NetworkServer.active) return;
                    onChanged(dropdown.value == 0);
                }, initialOn ? 0 : 1);
                SetInteractable(go, isHost);
            }
            SetLabel(go, label);
        }

        private static void SetLabel(GameObject go, string text)
        {
            var texts = go.GetComponentsInChildren<TMP_Text>(true);
            foreach (var t in texts)
            {
                if (t.gameObject.name == "Label Text")
                {
                    foreach (var comp in t.gameObject.GetComponents<Component>())
                        if (!(comp is Transform || comp is TMP_Text || comp is CanvasRenderer))
                            Object.Destroy(comp);
                    t.text = text;
                    return;
                }
            }
            if (texts.Length > 0) texts[0].text = text;
        }

        private static void SetInteractable(GameObject go, bool interactable)
        {
            foreach (var selectable in go.GetComponentsInChildren<Selectable>(true))
                selectable.interactable = interactable;
        }
    }
}