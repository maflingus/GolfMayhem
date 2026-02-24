using System.Collections.Generic;
using System.Reflection;
using Mirror;
using UnityEngine;

namespace GolfMayhem.ChaosEventSystem.Events
{
    public class GolfCartChaosEvent : ChaosEvent
    {
        private static MethodInfo _serverEnter;

        public override string DisplayName => "Golf Cart Chaos";
        public override string NetworkId => "GolfCartChaos";
        public override string WarnMessage => "Something's approaching...";
        public override string ActivateMessage => "Everyone now has golf carts!";
        public override float Weight => Configuration.WeightGolfCartChaos.Value;
        public override bool IsEnabled => Configuration.EnableGolfCartChaos.Value;

        public override void OnActivate()
        {
            if (!NetworkServer.active) return;

            if (_serverEnter == null)
            {
                _serverEnter = typeof(GolfCartInfo).GetMethod(
                    "ServerEnter",
                    BindingFlags.Instance | BindingFlags.NonPublic,
                    null, new[] { typeof(PlayerInfo) }, null);

                if (_serverEnter == null)
                {
                    GolfMayhemPlugin.Log.LogError("[GolfCartChaos] Could not find GolfCartInfo.ServerEnter!");
                    return;
                }
            }

            var prefab = GameManager.GolfCartSettings.Prefab;
            if (prefab == null)
            {
                GolfMayhemPlugin.Log.LogError("[GolfCartChaos] GolfCartSettings.Prefab is null!");
                return;
            }

            var players = new List<PlayerInfo>();
            if (GameManager.LocalPlayerInfo != null) players.Add(GameManager.LocalPlayerInfo);
            foreach (var p in GameManager.RemotePlayers) players.Add(p);

            int spawned = 0;
            foreach (var player in players)
            {
                if (player == null) continue;

                var cart = Object.Instantiate(
                    prefab,
                    player.transform.position,
                    Quaternion.Euler(0f, player.transform.eulerAngles.y, 0f));

                if (cart == null)
                {
                    GolfMayhemPlugin.Log.LogError($"[GolfCartChaos] Cart failed to instantiate for {player.Name}");
                    continue;
                }

                cart.ServerReserveDriverSeatPreNetworkSpawn(player);
                NetworkServer.Spawn(cart.gameObject);
                cart.ServerReserveDriverSeatPostNetworkSpawn();

                // Seat the player immediately — no reservation dance needed server-side
                try
                {
                    _serverEnter.Invoke(cart, new object[] { player });
                    spawned++;
                    GolfMayhemPlugin.Log.LogInfo($"[GolfCartChaos] Spawned and seated {player.Name}.");
                }
                catch (System.Exception ex)
                {
                    GolfMayhemPlugin.Log.LogError($"[GolfCartChaos] ServerEnter failed for {player.Name}: {ex.Message}");
                }
            }

            GolfMayhemPlugin.Log.LogInfo($"[GolfCartChaos] Done. {spawned}/{players.Count} players seated.");
        }

        public override void OnDeactivate()
        {
            GolfMayhemPlugin.Log.LogInfo("[GolfCartChaos] Deactivated.");
        }
    }
}