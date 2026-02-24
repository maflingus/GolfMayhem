using HarmonyLib;
using GolfMayhem.HUD;
using UnityEngine;

namespace GolfMayhem.Patches
{
    /// <summary>
    /// Patches for player state: spawning, death/KO, shot fired, cart collision.
    ///
    /// HOW TO FIND THESE CLASSES:
    /// - Open Assembly-CSharp.dll in dnSpy
    /// - Search for: "PlayerController", "Player", "GolfPlayer", "PlayerState"
    /// - Look for methods: OnShot(), OnHit(), OnKO(), OnDeath(), TakeDamage(), FireBall()
    /// - The PlayerController likely has a field like: playerName, playerId, score
    /// </summary>

    // ─────────────────────────────────────────────────────────────────
    // PATCH: Player fires a shot
    // ─────────────────────────────────────────────────────────────────

    // TODO: Replace class/method names after dnSpy research.
    //
    // [HarmonyPatch(typeof(PlayerController), "FireShot")]
    // public static class Patch_PlayerFiredShot
    // {
    //     [HarmonyPostfix]
    //     public static void Postfix(PlayerController __instance, float power)
    //     {
    //         // Example: detect max-power shots
    //         if (power >= 0.95f)
    //         {
    //             AnnouncerHUD.Instance?.ShowAnnouncement(
    //                 $"💥 {__instance.PlayerName} CRANKS IT FULL POWER!", Color.white);
    //         }
    //     }
    // }

    // ─────────────────────────────────────────────────────────────────
    // PATCH: Player hit by another player's ball
    // ─────────────────────────────────────────────────────────────────

    // TODO: Replace class/method names after dnSpy research.
    //
    // [HarmonyPatch(typeof(PlayerController), "OnHitByBall")]
    // public static class Patch_PlayerHitByBall
    // {
    //     [HarmonyPostfix]
    //     public static void Postfix(PlayerController __instance, PlayerController attacker)
    //     {
    //         string victim = __instance.PlayerName;
    //         string hitter = attacker?.PlayerName ?? "Someone";
    //
    //         GolfMayhemPlugin.Log.LogDebug($"[PlayerPatch] {hitter} hit {victim} with their ball!");
    //
    //         // Random callout from a pool
    //         string[] callouts = {
    //             $"😤 {hitter} SENDS {victim} FLYING!",
    //             $"🏌️ {victim} TAKES A BALL TO THE FACE!",
    //             $"💀 FORE! {victim} DIDN'T MOVE!",
    //         };
    //         string msg = callouts[UnityEngine.Random.Range(0, callouts.Length)];
    //         AnnouncerHUD.Instance?.ShowAnnouncement(msg, new Color(1f, 0.4f, 0f));
    //     }
    // }

    // ─────────────────────────────────────────────────────────────────
    // PATCH: Golf cart collision (ram event)
    // ─────────────────────────────────────────────────────────────────

    // TODO: Replace class/method names after dnSpy research.
    // Look for the golf cart class — probably GolfCart, CartController, VehicleController.
    //
    // [HarmonyPatch(typeof(GolfCart), "OnCollisionEnter")]
    // public static class Patch_CartCollision
    // {
    //     [HarmonyPostfix]
    //     public static void Postfix(GolfCart __instance, Collision collision)
    //     {
    //         var hitPlayer = collision.gameObject.GetComponent<PlayerController>();
    //         if (hitPlayer == null) return;
    //
    //         string[] callouts = {
    //             $"🚗 CART ATTACK! {hitPlayer.PlayerName} IS DOWN!",
    //             $"🏎️ HIT AND RUN ON THE FAIRWAY!",
    //             $"⚠️ {hitPlayer.PlayerName} JUST GOT RAMMED!",
    //         };
    //         string msg = callouts[UnityEngine.Random.Range(0, callouts.Length)];
    //         AnnouncerHUD.Instance?.ShowAnnouncement(msg, Color.red);
    //     }
    // }

    /// <summary>
    /// Placeholder — see comments above for implementation instructions.
    /// </summary>
    public static class PlayerPatches_Placeholder { }
}
