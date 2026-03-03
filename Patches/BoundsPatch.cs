using HarmonyLib;
using UnityEngine;

namespace GolfMayhem.ChaosEventSystem.Events
{

    [HarmonyPatch(typeof(LevelBoundsTracker), "OnAuthoritativeBoundsStateChanged")]
    public static class LevelBoundsTracker_RacePatch
    {
        public static bool Prefix(LevelBoundsTracker __instance)
        {
            if (!GolfCartRaceEvent.RaceActive) return true;

            var entity = __instance.GetComponent<Entity>();
            if (entity == null || !entity.IsGolfCart) return true;

            return false;
        }

        [HarmonyPatch(typeof(GolfCartInfo), nameof(GolfCartInfo.OnUpdate))]
        public static class GolfCartInfo_OnUpdate_RacePatch
        {
            public static bool Prefix(GolfCartInfo __instance)
            {
                if (!GolfCartRaceEvent.RaceActive) return true;

                return false;
            }
        }

        [HarmonyPatch(typeof(PlayerInfo), nameof(PlayerInfo.ExitGolfCart))]
        public static class PlayerInfo_ExitGolfCart_RacePatch
        {
            public static bool Prefix()
            {
                if (!GolfCartRaceEvent.RaceActive) return true;
                return false;
            }
        }
    }
}