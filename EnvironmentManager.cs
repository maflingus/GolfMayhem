using System.Collections;
using GolfMayhem.ChaosEventSystem.Events;
using Mirror;
using UnityEngine;

namespace GolfMayhem
{
    public class EnvironmentManager : MonoBehaviour
    {
        public static EnvironmentManager Instance { get; private set; }

        public static bool RainActive { get; private set; }
        public static bool NightTimeActive { get; private set; }

        private RainEvent _rainEvent;
        private NightTimeEvent _nightTimeEvent;

        private Coroutine _nightFadeCoroutine;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            _rainEvent = new RainEvent();
            _nightTimeEvent = new NightTimeEvent();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                DeactivateAll();
                Instance = null;
            }
        }

        public void BroadcastEnvironment(bool rain, bool nightTime)
        {
            if (!NetworkServer.active) return;
            GolfMayhemNetwork.SendEnvironment(rain, nightTime);
        }

        public void ApplyEnvironment(bool rain, bool nightTime)
        {
            if (rain && !RainActive)
            {
                RainActive = true;
                _rainEvent.OnActivate();
            }
            else if (!rain && RainActive)
            {
                RainActive = false;
                _rainEvent.OnDeactivate();
            }

            // Night time
            if (nightTime && !NightTimeActive)
            {
                NightTimeActive = true;
                _nightTimeEvent.OnActivate();
            }
            else if (!nightTime && NightTimeActive)
            {
                NightTimeActive = false;
                _nightTimeEvent.OnDeactivate();
            }
        }

        public void DeactivateAll()
        {
            if (RainActive)
            {
                RainActive = false;
                _rainEvent?.OnDeactivate();
            }
            if (NightTimeActive)
            {
                NightTimeActive = false;
                _nightTimeEvent?.OnDeactivate();
            }
        }

        public Coroutine StartManagedCoroutine(IEnumerator routine)
            => StartCoroutine(routine);

        public void StopManagedCoroutine(Coroutine c)
        {
            if (c != null) StopCoroutine(c);
        }
    }
}