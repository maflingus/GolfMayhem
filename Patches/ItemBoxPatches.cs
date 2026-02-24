using HarmonyLib;
using GolfMayhem.HUD;
using UnityEngine;

namespace GolfMayhem.Patches
{
    /// <summary>
    /// Patches for item box (suitcase) interactions.
    /// Hook these to react when players pick up items — show announcements,
    /// track item usage for stats, or inject extra effects.
    ///
    /// HOW TO FIND THESE CLASSES:
    /// - Open Assembly-CSharp.dll in dnSpy
    /// - Search for: "ItemBox", "Suitcase", "PowerUp", "Item", "Pickup"
    /// - Look for methods: OnPickup(), Use(), Collect(), OnTriggerEnter()
    /// - The item's type/name will tell you what to announce
    /// </summary>

    // ─────────────────────────────────────────────────────────────────
    // PATCH: Item Box Collected
    // ─────────────────────────────────────────────────────────────────

    // TODO: Replace with real class & method names from dnSpy.
    //
    // [HarmonyPatch(typeof(ItemBox), "OnCollected")]
    // public static class Patch_ItemBoxCollected
    // {
    //     [HarmonyPostfix]
    //     public static void Postfix(ItemBox __instance, PlayerController collector)
    //     {
    //         string itemName = __instance.ItemName; // Adjust field name
    //         string playerName = collector.PlayerName; // Adjust field name
    //
    //         GolfMayhemPlugin.Log.LogDebug($"[ItemBox] {playerName} picked up: {itemName}");
    //
    //         // Example: announce powerful/rare items
    //         if (itemName.ToLower().Contains("laser") || itemName.ToLower().Contains("orbital"))
    //         {
    //             AnnouncerHUD.Instance?.ShowAnnouncement(
    //                 $"☢️ {playerName} has the ORBITAL LASER!", Color.red);
    //         }
    //     }
    // }

    // ─────────────────────────────────────────────────────────────────
    // PATCH: Item Used
    // ─────────────────────────────────────────────────────────────────

    // TODO: Replace with real class & method names from dnSpy.
    //
    // [HarmonyPatch(typeof(ItemBox), "UseItem")]
    // public static class Patch_ItemUsed
    // {
    //     [HarmonyPrefix]
    //     public static bool Prefix(ItemBox __instance)
    //     {
    //         // Return false to cancel item use (e.g., during certain chaos events).
    //         // Return true to let it proceed normally.
    //         return true;
    //     }
    //
    //     [HarmonyPostfix]
    //     public static void Postfix(ItemBox __instance)
    //     {
    //         GolfMayhemPlugin.Log.LogDebug($"[ItemBox] Item used: {__instance.ItemName}");
    //     }
    // }

    /// <summary>
    /// Placeholder — see comments above for implementation instructions.
    /// </summary>
    public static class ItemBoxPatches_Placeholder { }
}
