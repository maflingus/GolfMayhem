using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GolfMayhem.ChaosEventSystem
{
    /// <summary>
    /// Manages the scheduling and dispatching of chaos events during a golf round.
    /// This MonoBehaviour lives on a persistent GameObject for the duration of the round.
    /// </summary>
    public class ChaosEventManager : MonoBehaviour
    {
        // ─────────────────────────────────────────────────────────────
        // Public access — other systems can check what's happening
        // ─────────────────────────────────────────────────────────────
        public static ChaosEventManager Instance { get; private set; }
        public static event Action<ChaosEvent> OnChaosEventStarted;
        public static event Action<ChaosEvent> OnChaosEventEnded;

        public ChaosEvent ActiveEvent { get; private set; }
        public bool IsEventActive => ActiveEvent != null;

        // ─────────────────────────────────────────────────────────────
        // Internal state
        // ─────────────────────────────────────────────────────────────
        private readonly List<ChaosEvent> _registeredEvents = new List<ChaosEvent>();
        private readonly System.Random _rng = new System.Random();
        private Coroutine _schedulerCoroutine;

        // ─────────────────────────────────────────────────────────────
        // Unity lifecycle
        // ─────────────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            RegisterBuiltInEvents();
            GolfMayhemPlugin.Log.LogInfo($"ChaosEventManager ready with {_registeredEvents.Count} events.");
        }

        private void Start()
        {
            _schedulerCoroutine = StartCoroutine(ChaosSchedulerRoutine());
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (_schedulerCoroutine != null) StopCoroutine(_schedulerCoroutine);
            ForceDeactivateCurrentEvent();
        }

        private void Update()
        {
            ActiveEvent?.OnUpdate();
        }

        // ─────────────────────────────────────────────────────────────
        // Event Registration
        // ─────────────────────────────────────────────────────────────

        private void RegisterBuiltInEvents()
        {
            Register(new Events.GravityFlipEvent());
            Register(new Events.SpeedSurgeEvent());
            Register(new Events.MineFloodEvent());
            Register(new Events.MagnetHoleEvent());
            Register(new Events.FogOfWarEvent());
        }

        /// <summary>
        /// Register a custom chaos event. Called by GolfMayhemAPI.
        /// Safe to call from any mod's Awake().
        /// </summary>
        public void Register(ChaosEvent evt)
        {
            if (evt == null) throw new ArgumentNullException(nameof(evt));
            _registeredEvents.Add(evt);
            GolfMayhemPlugin.Log.LogDebug($"Registered chaos event: {evt.DisplayName}");
        }

        // ─────────────────────────────────────────────────────────────
        // Scheduling
        // ─────────────────────────────────────────────────────────────

        private IEnumerator ChaosSchedulerRoutine()
        {
            // Brief grace period at round start before first event
            yield return new WaitForSeconds(15f);

            while (true)
            {
                float interval = UnityEngine.Random.Range(
                    Configuration.ChaosEventIntervalMin.Value,
                    Configuration.ChaosEventIntervalMax.Value);

                yield return new WaitForSeconds(interval);

                // Don't fire a new event if one is still running
                if (!IsEventActive)
                {
                    TriggerRandomEvent();
                }
            }
        }

        // ─────────────────────────────────────────────────────────────
        // Event Selection & Dispatch
        // ─────────────────────────────────────────────────────────────

        private void TriggerRandomEvent()
        {
            var pool = _registeredEvents.Where(e => e.IsEnabled && e.Weight > 0f).ToList();
            if (pool.Count == 0)
            {
                GolfMayhemPlugin.Log.LogWarning("No eligible chaos events to pick from.");
                return;
            }

            var chosen = WeightedRandom(pool);
            StartCoroutine(RunEventRoutine(chosen));
        }

        private IEnumerator RunEventRoutine(ChaosEvent evt)
        {
            GolfMayhemPlugin.Log.LogInfo($"[ChaosEvent] WARNING: {evt.DisplayName}");
            
            // Fire warning through the HUD system
            HUD.AnnouncerHUD.Instance?.ShowAnnouncement(evt.WarnMessage, Color.yellow);

            // 3-second warning window before the event fires
            yield return new WaitForSeconds(3f);

            // Activate
            GolfMayhemPlugin.Log.LogInfo($"[ChaosEvent] ACTIVATING: {evt.DisplayName}");
            ActiveEvent = evt;
            evt.OnActivate();
            OnChaosEventStarted?.Invoke(evt);
            HUD.AnnouncerHUD.Instance?.ShowAnnouncement(evt.ActivateMessage, Color.red);

            // Let it run
            yield return new WaitForSeconds(Configuration.ChaosEventDuration.Value);

            // Deactivate
            GolfMayhemPlugin.Log.LogInfo($"[ChaosEvent] DEACTIVATING: {evt.DisplayName}");
            evt.OnDeactivate();
            OnChaosEventEnded?.Invoke(evt);
            ActiveEvent = null;

            HUD.AnnouncerHUD.Instance?.ShowAnnouncement("✅ Chaos subsides... for now.", Color.green);
        }

        private void ForceDeactivateCurrentEvent()
        {
            if (ActiveEvent == null) return;
            try { ActiveEvent.OnDeactivate(); }
            catch (Exception ex) { GolfMayhemPlugin.Log.LogError($"Error deactivating event on destroy: {ex}"); }
            ActiveEvent = null;
        }

        // ─────────────────────────────────────────────────────────────
        // Weighted random selection
        // ─────────────────────────────────────────────────────────────

        private static ChaosEvent WeightedRandom(List<ChaosEvent> events)
        {
            float totalWeight = events.Sum(e => e.Weight);
            float roll = UnityEngine.Random.Range(0f, totalWeight);
            float cumulative = 0f;

            foreach (var evt in events)
            {
                cumulative += evt.Weight;
                if (roll <= cumulative) return evt;
            }

            return events[events.Count - 1]; // Fallback — should never hit
        }
    }
}
