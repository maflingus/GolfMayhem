using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

namespace GolfMayhem.ChaosEventSystem
{
    public class ChaosEventManager : MonoBehaviour
    {
        public static ChaosEventManager Instance { get; private set; }
        public static event Action<ChaosEvent> OnChaosEventStarted;
        public static event Action<ChaosEvent> OnChaosEventEnded;

        public ChaosEvent ActiveEvent { get; private set; }
        public bool IsEventActive => ActiveEvent != null;

        private readonly List<ChaosEvent> _registeredEvents = new List<ChaosEvent>();
        private Coroutine _schedulerCoroutine;
        private ChaosEvent _lastEvent;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            RegisterBuiltInEvents();
            GolfMayhemPlugin.Log.LogInfo($"ChaosEventManager ready with {_registeredEvents.Count} events.");
        }

        private void Start()
        {
            if (NetworkServer.active)
                _schedulerCoroutine = StartCoroutine(ChaosSchedulerRoutine());
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (_schedulerCoroutine != null) StopCoroutine(_schedulerCoroutine);
            ForceDeactivateCurrentEvent();
        }

        private void Update() { ActiveEvent?.OnUpdate(); }

        private void RegisterBuiltInEvents()
        {
            Register(new Events.GravityFlipEvent());
            Register(new Events.SpeedSurgeEvent());
            Register(new Events.MineFloodEvent());
            Register(new Events.MagnetHoleEvent());
            Register(new Events.FogOfWarEvent());
            Register(new Events.MiniatureEvent());
            Register(new Events.OrbitalStrikeEvent());
            Register(new Events.GiantModeEvent());
            Register(new Events.NightTimeEvent());
            Register(new Events.GolfCartChaosEvent());
            Register(new Events.CoffeeEvent());
            Register(new Events.TornadoEvent());
        }

        public void Register(ChaosEvent evt)
        {
            if (evt == null) throw new ArgumentNullException(nameof(evt));
            _registeredEvents.Add(evt);
        }

        public ChaosEvent GetEventByName(string networkId)
            => _registeredEvents.FirstOrDefault(e => e.NetworkId == networkId);

        private IEnumerator ChaosSchedulerRoutine()
        {
            yield return new WaitForSeconds(15f);
            while (true)
            {
                float interval = UnityEngine.Random.Range(
                    Configuration.ChaosEventIntervalMin.Value,
                    Configuration.ChaosEventIntervalMax.Value);
                yield return new WaitForSeconds(interval);
                if (!IsEventActive) TriggerRandomEvent();
            }
        }

        private void TriggerRandomEvent()
        {
            var pool = _registeredEvents.Where(e => e.IsEnabled && e.Weight > 0f && e != _lastEvent).ToList();
            if (pool.Count == 0)
                pool = _registeredEvents.Where(e => e.IsEnabled && e.Weight > 0f).ToList();
            if (pool.Count == 0) { GolfMayhemPlugin.Log.LogWarning("No eligible chaos events to pick from."); return; }
            var chosen = WeightedRandom(pool);
            _lastEvent = chosen;
            StartCoroutine(RunEventRoutine(chosen));
        }

        private IEnumerator RunEventRoutine(ChaosEvent evt)
        {
            GolfMayhemPlugin.Log.LogInfo($"[ChaosEvent] WARNING: {evt.DisplayName}");

            // Show warning in chat on all clients
            GolfMayhemNetwork.SendWarn(evt.NetworkId, evt.WarnMessage);

            yield return new WaitForSeconds(3f);

            // Activate on host
            GolfMayhemPlugin.Log.LogInfo($"[ChaosEvent] ACTIVATING: {evt.DisplayName}");
            ActiveEvent = evt;
            evt.OnActivate();
            OnChaosEventStarted?.Invoke(evt);

            // Show activation in chat + trigger clients
            GolfMayhemNetwork.SendActivate(evt.NetworkId, evt.ActivateMessage);

            yield return new WaitForSeconds(Configuration.ChaosEventDuration.Value);

            // Deactivate on host
            GolfMayhemPlugin.Log.LogInfo($"[ChaosEvent] DEACTIVATING: {evt.DisplayName}");
            evt.OnDeactivate();
            OnChaosEventEnded?.Invoke(evt);
            ActiveEvent = null;

            // Show all-clear in chat + trigger clients
            GolfMayhemNetwork.SendDeactivate(evt.NetworkId, "Chaos subsides... for now.");
        }

        public void ActivateEventLocally(ChaosEvent evt)
        {
            ActiveEvent = evt;
            evt.OnActivate();
            OnChaosEventStarted?.Invoke(evt);
        }

        public void DeactivateEventLocally(ChaosEvent evt)
        {
            evt.OnDeactivate();
            OnChaosEventEnded?.Invoke(evt);
            ActiveEvent = null;
        }

        private void ForceDeactivateCurrentEvent()
        {
            if (ActiveEvent == null) return;
            try { ActiveEvent.OnDeactivate(); }
            catch (Exception ex) { GolfMayhemPlugin.Log.LogError($"Error deactivating on destroy: {ex}"); }
            ActiveEvent = null;
        }

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
            return events[events.Count - 1];
        }
    }
}