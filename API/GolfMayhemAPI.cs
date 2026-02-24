using GolfMayhem.ChaosEventSystem;
using UnityEngine;

namespace GolfMayhem.API
{
    /// <summary>
    /// Public API surface for other mods to integrate with GolfMayhem.
    ///
    /// USAGE EXAMPLE (in another BepInEx mod):
    ///
    ///   [BepInDependency("com.golfmayhem.superbattlegolf", BepInDependency.DependencyFlags.SoftDependency)]
    ///   public class MyMod : BaseUnityPlugin
    ///   {
    ///       private void Awake()
    ///       {
    ///           GolfMayhemAPI.RegisterEvent(new MyCustomChaosEvent());
    ///           GolfMayhemAPI.OnChaosEventStarted += OnChaosStarted;
    ///       }
    ///
    ///       private void OnChaosStarted(ChaosEvent evt)
    ///       {
    ///           if (evt is GravityFlipEvent)
    ///               DoSomethingSpecial();
    ///       }
    ///   }
    /// </summary>
    public static class GolfMayhemAPI
    {
        // ── Events (mirrors ChaosEventManager's events for external listeners) ──
        public static event System.Action<ChaosEvent> OnChaosEventStarted
        {
            add => ChaosEventManager.OnChaosEventStarted += value;
            remove => ChaosEventManager.OnChaosEventStarted -= value;
        }

        public static event System.Action<ChaosEvent> OnChaosEventEnded
        {
            add => ChaosEventManager.OnChaosEventEnded += value;
            remove => ChaosEventManager.OnChaosEventEnded -= value;
        }

        // ── Registration ─────────────────────────────────────────────

        /// <summary>
        /// Register a custom chaos event to be included in the chaos pool.
        /// Call this from your mod's Awake() or Start().
        /// Silently fails if ChaosEventManager isn't ready yet (retry on scene load).
        /// </summary>
        public static bool RegisterEvent(ChaosEvent evt)
        {
            if (ChaosEventManager.Instance == null)
            {
                GolfMayhemPlugin.Log.LogWarning(
                    $"[API] RegisterEvent({evt?.DisplayName}) called before ChaosEventManager is ready. " +
                    "Try registering from OnSceneLoaded instead.");
                return false;
            }

            ChaosEventManager.Instance.Register(evt);
            return true;
        }

        // ── Queries ──────────────────────────────────────────────────

        /// <summary>Returns the currently active chaos event, or null if none.</summary>
        public static ChaosEvent GetActiveEvent() => ChaosEventManager.Instance?.ActiveEvent;

        /// <summary>Returns true if any chaos event is currently running.</summary>
        public static bool IsChaosActive() => ChaosEventManager.Instance?.IsEventActive ?? false;
    }
}
