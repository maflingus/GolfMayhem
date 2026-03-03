using System;
using System.Collections;
using UnityEngine;

namespace GolfMayhem.ChaosEventSystem.Events
{
    public class VfxTestEvent : ChaosEvent
    {
        public override string DisplayName => "VFX Test";
        public override string NetworkId => "VfxTest";
        public override string WarnMessage => "VFX test starting...";
        public override string ActivateMessage => "VFX test — cycling all particle types.";
        public override float Weight => Configuration.WeightVfxTest.Value;
        public override bool IsEnabled => Configuration.EnableVfxTest.Value;
        public override float Duration => float.MaxValue; // self-terminates

        private Coroutine _cycleCoroutine;

        public override void OnActivate()
        {
            _cycleCoroutine = ChaosEventManager.Instance.StartCoroutine(CycleRoutine());
        }

        public override void OnDeactivate()
        {
            if (_cycleCoroutine != null)
                ChaosEventManager.Instance?.StopCoroutine(_cycleCoroutine);
        }

        private IEnumerator CycleRoutine()
        {
            var types = (VfxType[])Enum.GetValues(typeof(VfxType));

            foreach (var vfxType in types)
            {
                if (vfxType == VfxType.None) continue;

                var player = GameManager.LocalPlayerInfo;
                Vector3 spawnPos = player != null
                    ? player.transform.position + player.transform.forward * 3f + Vector3.up * 1f
                    : Vector3.zero;

                GolfMayhemPlugin.Log.LogInfo($"[VfxTest] Playing: {vfxType}");
                GolfMayhemNetwork.SendWarn(NetworkId, $"VFX: {vfxType}");

                PoolableParticleSystem vfx = null;
                try
                {
                    vfx = VfxManager.PlayPooledVfxLocalOnly(vfxType, spawnPos, Quaternion.identity);
                }
                catch (Exception ex)
                {
                    GolfMayhemPlugin.Log.LogWarning($"[VfxTest] Failed to play {vfxType}: {ex.Message}");
                }

                yield return new WaitForSeconds(5f);

                try { vfx?.Stop(ParticleSystemStopBehavior.StopEmittingAndClear); }
                catch { }
            }

            GolfMayhemPlugin.Log.LogInfo("[VfxTest] All VFX cycled.");
            ChaosEventManager.Instance?.DeactivateEventLocally(this);
            GolfMayhemNetwork.SendDeactivate(NetworkId, "VFX test complete.");
        }
    }
}