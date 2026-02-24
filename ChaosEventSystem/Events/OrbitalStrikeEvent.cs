using Mirror;
using UnityEngine;

namespace GolfMayhem.ChaosEventSystem.Events
{

    public class OrbitalStrikeEvent : ChaosEvent
    {
        private static int _chaosItemUseIndex = 8000;

        public override string DisplayName => "Orbital Strike";
        public override string NetworkId => "OrbitalStrike";
        public override string WarnMessage => "Something is locking on...";
        public override string ActivateMessage => "Orbital Strike! Take cover!";
        public override float Weight => Configuration.WeightOrbitalStrike.Value;
        public override bool IsEnabled => Configuration.EnableOrbitalStrike.Value;

        public override void OnActivate()
        {
            if (!NetworkServer.active) return;

            var localInfo = GameManager.LocalPlayerInfo;
            if (localInfo == null || localInfo.Inventory == null)
            {
                GolfMayhemPlugin.Log.LogWarning("[OrbitalStrike] No local player or inventory.");
                return;
            }

            int fired = 0;

            FireAt(GameManager.LocalPlayerInfo, localInfo);

            foreach (var remotePlayer in GameManager.RemotePlayers)
            {
                FireAt(remotePlayer, localInfo);
            }

            GolfMayhemPlugin.Log.LogInfo($"[OrbitalStrike] Fired {fired} lasers.");

            void FireAt(PlayerInfo target, PlayerInfo owner)
            {
                if (target == null) return;
                if (!OrbitalLaserManager.CanTarget(target.AsHittable)) return;

                var itemUseId = new ItemUseId(owner.Guid, _chaosItemUseIndex++, ItemType.OrbitalLaser);
                OrbitalLaserManager.ServerActivateLaser(
                    target.AsHittable,
                    target.transform.position,
                    owner.Inventory,
                    itemUseId
                );
                fired++;
            }
        }

        public override void OnDeactivate()
        {
            GolfMayhemPlugin.Log.LogInfo("[OrbitalStrike] Deactivated.");
        }
    }
}