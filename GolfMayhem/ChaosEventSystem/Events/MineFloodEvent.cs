using UnityEngine;
using Mirror;

namespace GolfMayhem.ChaosEventSystem.Events
{
    public class MineFloodEvent : ChaosEvent
    {
        private const int MINE_COUNT = 12;
        private const float SPAWN_RADIUS = 30f;
        private const float MINE_HEIGHT_OFFSET = 0.3f;

        private static int _mineUseIdCounter = int.MinValue;

        public override string DisplayName => "Mine Flood";
        public override string WarnMessage => "Landmines spotted near the hole!";
        public override string ActivateMessage => "Be careful when putting!";
        public override float Weight => Configuration.WeightMineFlood.Value;
        public override bool IsEnabled => Configuration.EnableMineFlood.Value;

        public override void OnActivate()
        {

            if (!NetworkServer.active)
            {
                GolfMayhemPlugin.Log.LogWarning("[MineFlood] Not server — skipping spawn.");
                return;
            }

            var localPlayerInfo = GameManager.LocalPlayerInfo;
            if (localPlayerInfo == null)
            {
                GolfMayhemPlugin.Log.LogWarning("[MineFlood] LocalPlayerInfo is null — skipping.");
                return;
            }

            PlayerInventory inventory = localPlayerInfo.Inventory;
            
            ulong ownerGuid = localPlayerInfo.Guid;

            Vector3 center = GolfHoleManager.MainHole != null
                ? GolfHoleManager.MainHole.transform.position
                : Vector3.zero;

            int spawned = 0;
            for (int i = 0; i < MINE_COUNT; i++)
            {
                Vector3 pos = GetRandomGroundPosition(center);

                ItemUseId itemUseId = new ItemUseId(ownerGuid, ++_mineUseIdCounter, ItemType.Landmine);

                CourseManager.CmdSpawnLandmine(
                    pos,
                    Quaternion.identity,
                    Vector3.zero,
                    Vector3.zero,
                    LandmineArmType.Planted,
                    inventory,
                    itemUseId
                );

                spawned++;
            }

            GolfMayhemPlugin.Log.LogInfo($"[MineFlood] Spawned {spawned} real landmines around {center}.");
        }

        public override void OnDeactivate()
        {
            GolfMayhemPlugin.Log.LogInfo("[MineFlood] Deactivated — mines will explode naturally.");
        }

        public override void OnUpdate() { }

        private static Vector3 GetRandomGroundPosition(Vector3 center)
        {
            Vector2 disc = Random.insideUnitCircle * SPAWN_RADIUS;
            Vector3 candidate = new Vector3(center.x + disc.x, center.y + 20f, center.z + disc.y);

            if (Physics.Raycast(candidate, Vector3.down, out RaycastHit hit, 60f))
            {
                return hit.point + Vector3.up * MINE_HEIGHT_OFFSET;
            }

            return new Vector3(candidate.x, center.y + MINE_HEIGHT_OFFSET, candidate.z);
        }
    }
}