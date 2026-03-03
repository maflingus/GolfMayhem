using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace GolfMayhem.ChaosEventSystem.Events
{
    public class GauntletEvent : ChaosEvent
    {
        // Arena dimensions
        private const float ARENA_HEIGHT = 60f;   // height above ground
        private const float ARENA_SIZE = 36f;   // width/depth
        private const float ARENA_WALL_H = 16f;   // wall height
        private const float ARENA_THICKNESS = 0.5f;  // wall thickness
        private const float ARENA_SPACING = 60f;   // horizontal gap between multiple arenas
        private const float DUEL_DISTANCE = 18f;   // how far apart the two players stand

        private class DuelState
        {
            public PlayerInfo PlayerA;
            public PlayerInfo PlayerB;
            public Vector3 OriginalPosA;
            public Vector3 OriginalPosB;
            public Quaternion OriginalRotA;
            public Quaternion OriginalRotB;
            public List<GameObject> ArenaWalls = new List<GameObject>();
            public bool Resolved;
        }

        private readonly List<DuelState> _activeDuels = new List<DuelState>();
        private readonly List<Coroutine> _watchCoroutines = new List<Coroutine>();

        public override string DisplayName => "Gauntlet";
        public override string NetworkId => "Gauntlet";
        public override string WarnMessage => "Prepare for the 1v1 arena...";
        public override string ActivateMessage => "1v1 Arena! Fight to the death!";
        public override float Weight => Configuration.WeightGauntlet.Value;
        public override bool IsEnabled => Configuration.EnableGauntlet.Value;

        public override void OnActivate()
        {
            var players = new List<PlayerInfo>();
            if (GameManager.LocalPlayerInfo != null) players.Add(GameManager.LocalPlayerInfo);
            foreach (var p in new List<PlayerInfo>(GameManager.RemotePlayers))
                if (p != null) players.Add(p);

            GolfMayhemPlugin.Log.LogInfo($"[Gauntlet] Found {players.Count} players.");

            if (players.Count < 1)
            {
                GolfMayhemPlugin.Log.LogWarning("[Gauntlet] No players found, aborting.");
                return;
            }

            for (int i = players.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (players[i], players[j]) = (players[j], players[i]);
            }

            _activeDuels.Clear();
            _watchCoroutines.Clear();

            if (players.Count == 1)
                players.Add(players[0]);

            int arenaIndex = 0;
            for (int i = 0; i + 1 < players.Count; i += 2)
            {
                var duel = new DuelState
                {
                    PlayerA = players[i],
                    PlayerB = players[i + 1]
                };
                _activeDuels.Add(duel);

                Vector3 midpoint = (duel.OriginalPosA + duel.OriginalPosB) / 2f;
                Vector3 arenaCenter = new Vector3(midpoint.x + arenaIndex * ARENA_SPACING, midpoint.y + ARENA_HEIGHT, midpoint.z);
                StartDuel(duel, arenaCenter);
                arenaIndex++;
            }

            if (players.Count % 2 != 0 && players.Count > 0)
                GolfMayhemPlugin.Log.LogInfo($"[Gauntlet] {players[players.Count - 1].Name} sits this round out.");
        }

        private void StartDuel(DuelState duel, Vector3 arenaCenter)
        {
            duel.OriginalPosA = duel.PlayerA.Rigidbody.position;
            duel.OriginalPosB = duel.PlayerB.Rigidbody.position;
            duel.OriginalRotA = duel.PlayerA.transform.rotation;
            duel.OriginalRotB = duel.PlayerB.transform.rotation;

            if (NetworkServer.active)
            {
                BuildArena(duel, arenaCenter);
                BuildObstacles(duel, arenaCenter);
            }

            var localPlayer = GameManager.LocalPlayerInfo;

            if (localPlayer != null && (localPlayer == duel.PlayerA || localPlayer == duel.PlayerB))
            {
                BoundsManager.DeregisterLevelBoundsTracker(localPlayer.LevelBoundsTracker);
            }

            Vector3 posA = arenaCenter + new Vector3(-DUEL_DISTANCE / 2f, 1f, 0f);
            Vector3 posB = arenaCenter + new Vector3(DUEL_DISTANCE / 2f, 1f, 0f);
            Quaternion rotA = Quaternion.LookRotation(Vector3.right);
            Quaternion rotB = Quaternion.LookRotation(Vector3.left);

            if (localPlayer == duel.PlayerA)
                localPlayer.Movement.Teleport(posA, rotA, resetState: true);
            else if (localPlayer == duel.PlayerB)
                localPlayer.Movement.Teleport(posB, rotB, resetState: true);

            if (localPlayer != null && (localPlayer == duel.PlayerA || localPlayer == duel.PlayerB))
            {
                localPlayer.Inventory.ServerTryAddItem(ItemType.DuelingPistol, 999);
            }

            var coroutine = ChaosEventManager.Instance.StartCoroutine(WatchDuel(duel, posA, posB, rotA, rotB));
            _watchCoroutines.Add(coroutine);
        }

        private void BuildArena(DuelState duel, Vector3 center)
        {
            float halfSize = ARENA_SIZE / 2f;
            float halfH = ARENA_WALL_H / 2f;
            float halfT = ARENA_THICKNESS / 2f;

            duel.ArenaWalls.Add(CreateWall(
                center + new Vector3(0f, -halfT, 0f),
                new Vector3(ARENA_SIZE, ARENA_THICKNESS, ARENA_SIZE)));

            duel.ArenaWalls.Add(CreateWall(
                center + new Vector3(0f, ARENA_WALL_H + halfT, 0f),
                new Vector3(ARENA_SIZE, ARENA_THICKNESS, ARENA_SIZE)));

            duel.ArenaWalls.Add(CreateWall(
                center + new Vector3(0f, halfH, halfSize + halfT),
                new Vector3(ARENA_SIZE, ARENA_WALL_H, ARENA_THICKNESS)));

            duel.ArenaWalls.Add(CreateWall(
                center + new Vector3(0f, halfH, -halfSize - halfT),
                new Vector3(ARENA_SIZE, ARENA_WALL_H, ARENA_THICKNESS)));

            duel.ArenaWalls.Add(CreateWall(
                center + new Vector3(-halfSize - halfT, halfH, 0f),
                new Vector3(ARENA_THICKNESS, ARENA_WALL_H, ARENA_SIZE)));

            duel.ArenaWalls.Add(CreateWall(
                center + new Vector3(halfSize + halfT, halfH, 0f),
                new Vector3(ARENA_THICKNESS, ARENA_WALL_H, ARENA_SIZE)));
        }

        private void BuildObstacles(DuelState duel, Vector3 center)
        {
            duel.ArenaWalls.Add(CreateWall(
                center + new Vector3(-3f, 2f, 0f),
                new Vector3(1.5f, 4f, 6f)));
            duel.ArenaWalls.Add(CreateWall(
                center + new Vector3(3f, 2f, 0f),
                new Vector3(1.5f, 4f, 6f)));

            duel.ArenaWalls.Add(CreateWall(
                center + new Vector3(-10f, 2f, -8f),
                new Vector3(2f, 4f, 2f)));
            duel.ArenaWalls.Add(CreateWall(
                center + new Vector3(-10f, 2f, 8f),
                new Vector3(2f, 4f, 2f)));
            duel.ArenaWalls.Add(CreateWall(
                center + new Vector3(10f, 2f, -8f),
                new Vector3(2f, 4f, 2f)));
            duel.ArenaWalls.Add(CreateWall(
                center + new Vector3(10f, 2f, 8f),
                new Vector3(2f, 4f, 2f)));

            duel.ArenaWalls.Add(CreateWall(
                center + new Vector3(-7f, 1f, 0f),
                new Vector3(1f, 2f, 10f)));
            duel.ArenaWalls.Add(CreateWall(
                center + new Vector3(7f, 1f, 0f),
                new Vector3(1f, 2f, 10f)));
        }

        private GameObject CreateWall(Vector3 position, Vector3 scale)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.position = position;
            wall.transform.localScale = scale;
            wall.layer = LayerMask.NameToLayer("Default");

            var renderer = wall.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = new Material(Shader.Find("Standard"));
                renderer.material.color = new Color(0.15f, 0.15f, 0.15f);
            }

            return wall;
        }

        private IEnumerator WatchDuel(DuelState duel, Vector3 posA, Vector3 posB, Quaternion rotA, Quaternion rotB)
        {
            bool aKnockedOut = false;
            bool bKnockedOut = false;

            void OnAChanged()
            {
                if (duel.PlayerA.Movement.IsKnockedOutOrRecovering) aKnockedOut = true;
            }
            void OnBChanged()
            {
                if (duel.PlayerB.Movement.IsKnockedOutOrRecovering) bKnockedOut = true;
            }

            duel.PlayerA.Movement.IsKnockedOutOrRecoveringChanged += OnAChanged;
            duel.PlayerB.Movement.IsKnockedOutOrRecoveringChanged += OnBChanged;

            while (!aKnockedOut && !bKnockedOut && !duel.Resolved)
                yield return null;

            duel.PlayerA.Movement.IsKnockedOutOrRecoveringChanged -= OnAChanged;
            duel.PlayerB.Movement.IsKnockedOutOrRecoveringChanged -= OnBChanged;

            if (duel.Resolved) yield break;
            duel.Resolved = true;

            string winner = aKnockedOut ? duel.PlayerB.Name : duel.PlayerA.Name;
            string loser = aKnockedOut ? duel.PlayerA.Name : duel.PlayerB.Name;
            GolfMayhemPlugin.Log.LogInfo($"[Gauntlet] {winner} wins! {loser} is eliminated.");

            yield return new WaitForSeconds(2f);

            var localPlayer = GameManager.LocalPlayerInfo;
            bool localIsLoser = (aKnockedOut && localPlayer == duel.PlayerA) ||
                                (bKnockedOut && localPlayer == duel.PlayerB);

            if (localIsLoser)
            {
                BoundsManager.RegisterLevelBoundsTracker(localPlayer.LevelBoundsTracker);
                localPlayer.Movement.TryBeginRespawn(isRestart: true);
            }
            else if (localPlayer == duel.PlayerA || localPlayer == duel.PlayerB)
            {
                BoundsManager.RegisterLevelBoundsTracker(localPlayer.LevelBoundsTracker);
                Vector3 returnPos = (localPlayer == duel.PlayerA) ? duel.OriginalPosA : duel.OriginalPosB;
                Quaternion returnRot = (localPlayer == duel.PlayerA) ? duel.OriginalRotA : duel.OriginalRotB;
                localPlayer.Movement.Teleport(returnPos, returnRot, resetState: true);
            }

            if (NetworkServer.active)
                foreach (var wall in duel.ArenaWalls)
                    if (wall != null) Object.Destroy(wall);

            duel.ArenaWalls.Clear();
        }

        public override void OnDeactivate()
        {
            var host = ChaosEventManager.Instance;
            foreach (var c in _watchCoroutines)
                if (c != null && host != null) host.StopCoroutine(c);
            _watchCoroutines.Clear();

            foreach (var duel in _activeDuels)
            {
                duel.Resolved = true;

                var local = GameManager.LocalPlayerInfo;
                if (local == duel.PlayerA)
                {
                    local.Movement.Teleport(duel.OriginalPosA, duel.OriginalRotA, resetState: true);
                    BoundsManager.RegisterLevelBoundsTracker(local.LevelBoundsTracker);
                }
                else if (local == duel.PlayerB)
                {
                    local.Movement.Teleport(duel.OriginalPosB, duel.OriginalRotB, resetState: true);
                    BoundsManager.RegisterLevelBoundsTracker(local.LevelBoundsTracker);
                }

                if (NetworkServer.active)
                    foreach (var wall in duel.ArenaWalls)
                        if (wall != null) Object.Destroy(wall);

                duel.ArenaWalls.Clear();
            }

            _activeDuels.Clear();
            GolfMayhemPlugin.Log.LogInfo("[Gauntlet] Deactivated.");
        }
    }
}