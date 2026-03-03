using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Mirror;
using UnityEngine;

namespace GolfMayhem.ChaosEventSystem.Events
{
    public class GolfCartRaceEvent : ChaosEvent
    {

        private const float TRACK_HEIGHT = 100f;
        private const float TRACK_WIDTH = 42f;
        private const float TRACK_THICKNESS = 0.8f;
        private const float WALL_HEIGHT = 4f;
        private const float WALL_THICKNESS = 1.2f;
        private const float LOOP_W = 160f;
        private const float LOOP_D = 80f;
        private const int CURVE_SEG = 24;
        private const float GRID_SPACING_Z = 10f;

        private NightTimeEvent night;

        private static readonly Color COL_ROAD = new Color(0.15f, 0.15f, 0.18f);
        private static readonly Color COL_KERB_R = new Color(0.9f, 0.1f, 0.1f);
        private static readonly Color COL_KERB_W = new Color(0.95f, 0.95f, 0.95f);
        private static readonly Color COL_START = new Color(0.95f, 0.85f, 0.1f);
        private static readonly Color COL_FINISH = new Color(0.1f, 0.8f, 0.2f);
        private static readonly Color COL_BARRIER = new Color(0.85f, 0.5f, 0.05f);
        private static readonly Color COL_CHECKPOINT = new Color(0.6f, 0.1f, 0.9f);

        private static MethodInfo _serverEnter;

        private readonly List<GameObject> _trackObjects = new List<GameObject>();
        private readonly List<GolfCartInfo> _raceCarts = new List<GolfCartInfo>();
        private readonly List<PlayerInfo> _racers = new List<PlayerInfo>();
        private readonly List<PlayerInfo> _finishOrder = new List<PlayerInfo>();
        private readonly HashSet<PlayerInfo> _checkpointPassed = new HashSet<PlayerInfo>();
        private readonly Dictionary<PlayerInfo, int> _lapCounts = new Dictionary<PlayerInfo, int>();
        private readonly Dictionary<GolfCartInfo, RigidbodyConstraints> _origConstraints = new Dictionary<GolfCartInfo, RigidbodyConstraints>();

        private GameObject _finishTriggerObj;
        private GameObject _checkpointTriggerObj;
        private Vector3 _trackOrigin;
        private Coroutine _raceCoroutine;

        public static bool RaceActive { get; private set; }

        public override string DisplayName => "Golf Cart Race";
        public override string NetworkId => "GolfCartRace";
        public override string WarnMessage => "Start your engines...";
        public override string ActivateMessage => "Golf Cart race! First to complete one lap wins!";
        public override float Weight => Configuration.WeightGolfCartRace.Value;
        public override bool IsEnabled => Configuration.EnableGolfCartRace.Value;
        public override float Duration => float.MaxValue;

        public override void OnActivate()
        {
            if (RaceActive)
            {
                GolfMayhemPlugin.Log.LogWarning("[GolfCartRace] OnActivate called while already active — ignoring.");
                return;
            }
            RaceActive = true;

            _trackOrigin = new Vector3(
                GolfHoleManager.MainHole.transform.position.x,
                TRACK_HEIGHT,
                GolfHoleManager.MainHole.transform.position.z);

            var localPlayer = GameManager.LocalPlayerInfo;
            if (localPlayer != null)
                BoundsManager.DeregisterLevelBoundsTracker(localPlayer.LevelBoundsTracker);

            _trackObjects.Clear();
            BuildTrack();

            if (!NetworkServer.active) return;
            if (!CacheServerEnter()) return;

            _racers.Clear();
            _lapCounts.Clear();
            _finishOrder.Clear();
            _raceCarts.Clear();
            _checkpointPassed.Clear();

            if (GameManager.LocalPlayerInfo != null) _racers.Add(GameManager.LocalPlayerInfo);
            foreach (var p in new List<PlayerInfo>(GameManager.RemotePlayers))
                if (p != null) _racers.Add(p);

            _racers.Sort((a, b) => a.Guid.CompareTo(b.Guid));

            foreach (var p in _racers)
                _lapCounts[p] = 0;

            for (int i = 0; i < _racers.Count; i++)
                _racers[i].Movement.RpcInformSpawned(GetGridPosition(i), Quaternion.Euler(0, 90f, 0));

            night = new NightTimeEvent();
            night.OnActivate();
            _raceCoroutine = ChaosEventManager.Instance.StartCoroutine(RaceRoutine());
        }

        public override void OnDeactivate()
        {
            if (!RaceActive) return;
            RaceActive = false;

            if (_raceCoroutine != null)
                ChaosEventManager.Instance?.StopCoroutine(_raceCoroutine);

            night.OnDeactivate();

            UnsubscribeTriggers();
            FreezeAllCarts(false);
            Cleanup();
        }

        private void SubscribeTriggers()
        {
            if (_finishTriggerObj != null)
            {
                var ft = _finishTriggerObj.GetComponent<FinishLineTrigger>();
                if (ft != null)
                {
                    ft.ClearCooldowns();
                    ft.OnCartCrossed += OnCartCrossedFinishLine;
                }
            }
            if (_checkpointTriggerObj != null)
            {
                var ct = _checkpointTriggerObj.GetComponent<CheckpointTrigger>();
                if (ct != null)
                {
                    ct.ClearCooldowns();
                    ct.OnCartCrossed += OnCartCrossedCheckpoint;
                }
            }
        }

        private void UnsubscribeTriggers()
        {
            if (_finishTriggerObj != null)
            {
                var ft = _finishTriggerObj.GetComponent<FinishLineTrigger>();
                if (ft != null) ft.OnCartCrossed -= OnCartCrossedFinishLine;
            }
            if (_checkpointTriggerObj != null)
            {
                var ct = _checkpointTriggerObj.GetComponent<CheckpointTrigger>();
                if (ct != null) ct.OnCartCrossed -= OnCartCrossedCheckpoint;
            }
        }

        private void BuildTrack()
        {
            float hw = LOOP_W * 0.5f;
            float hd = LOOP_D * 0.5f;

            BuildStraight(new Vector3(-hw, 0, hd), new Vector3(hw, 0, hd), false);
            BuildStraight(new Vector3(-hw, 0, -hd), new Vector3(hw, 0, -hd), true);
            BuildCurve(new Vector3(-hw, 0, 0), hd, 90f, 270f, false);
            BuildCurve(new Vector3(hw, 0, 0), hd, 270f, 90f, true);

            float startX = -hw + 20f;
            BuildStartLine(new Vector3(startX, 0, hd));
            BuildFinishLine(new Vector3(startX + TRACK_WIDTH + 4f, 0, hd));
            BuildCheckpoint(new Vector3(startX + TRACK_WIDTH + 4f, 0, -hd));
        }

        private void BuildStraight(Vector3 a, Vector3 b, bool kerbAlt, float overlapFactor = 1.0f)
        {
            Vector3 dir = b - a;
            float len = dir.magnitude * overlapFactor;
            Vector3 ctr = (a + b) * 0.5f;
            float yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            var rot = Quaternion.Euler(0, yaw, 0);
            Vector3 right = rot * Vector3.right;
            float kw = WALL_THICKNESS;
            Color kc = kerbAlt ? COL_KERB_W : COL_KERB_R;

            AddBlock(_trackOrigin + ctr, new Vector3(TRACK_WIDTH, TRACK_THICKNESS, len), rot, COL_ROAD);
            AddBlock(_trackOrigin + ctr + right * (TRACK_WIDTH * 0.5f + kw * 0.5f), new Vector3(kw, WALL_HEIGHT, len), rot, kc);
            AddBlock(_trackOrigin + ctr - right * (TRACK_WIDTH * 0.5f + kw * 0.5f), new Vector3(kw, WALL_HEIGHT, len), rot, kc);
            AddBlock(_trackOrigin + ctr + right * (TRACK_WIDTH * 0.5f + kw * 0.5f) + Vector3.up * WALL_HEIGHT, new Vector3(kw * 0.4f, 0.6f, len), rot, COL_BARRIER);
            AddBlock(_trackOrigin + ctr - right * (TRACK_WIDTH * 0.5f + kw * 0.5f) + Vector3.up * WALL_HEIGHT, new Vector3(kw * 0.4f, 0.6f, len), rot, COL_BARRIER);
        }

        private void BuildCurve(Vector3 centre, float radius, float startDeg, float endDeg, bool kerbAlt)
        {
            float range = endDeg - startDeg;
            if (range < 0) range += 360f;
            float step = range / CURVE_SEG;
            for (int i = 0; i < CURVE_SEG; i++)
            {
                float a1 = (startDeg + step * i) * Mathf.Deg2Rad;
                float a2 = (startDeg + step * (i + 1)) * Mathf.Deg2Rad;
                Vector3 p1 = centre + new Vector3(Mathf.Cos(a1) * radius, 0, Mathf.Sin(a1) * radius);
                Vector3 p2 = centre + new Vector3(Mathf.Cos(a2) * radius, 0, Mathf.Sin(a2) * radius);
                BuildStraight(p1, p2, i % 2 == 0 ? kerbAlt : !kerbAlt, 2.0f);
            }
        }

        private void BuildStartLine(Vector3 rel)
        {
            AddBlock(_trackOrigin + rel + new Vector3(0, TRACK_THICKNESS * 0.5f + 0.02f, -TRACK_WIDTH * 0.25f),
                     new Vector3(2f, 0.05f, TRACK_WIDTH * 0.5f), Quaternion.identity, COL_START);
            AddBlock(_trackOrigin + rel + new Vector3(0, TRACK_THICKNESS * 0.5f + 0.02f, TRACK_WIDTH * 0.25f),
                     new Vector3(2f, 0.05f, TRACK_WIDTH * 0.5f), Quaternion.identity, COL_KERB_W);
        }

        private void BuildFinishLine(Vector3 rel)
        {
            Vector3 world = _trackOrigin + rel;

            _finishTriggerObj = new GameObject("GolfCartRaceFinishLine");
            _finishTriggerObj.transform.position = world + Vector3.up * 2f;
            _finishTriggerObj.transform.rotation = Quaternion.identity;
            _finishTriggerObj.transform.localScale = new Vector3(2f, 4f, TRACK_WIDTH);
            _finishTriggerObj.AddComponent<BoxCollider>().isTrigger = true;
            _finishTriggerObj.AddComponent<FinishLineTrigger>();

            AddBlock(world + new Vector3(0, TRACK_THICKNESS * 0.5f + 0.02f, 0),
                     new Vector3(2f, 0.05f, TRACK_WIDTH), Quaternion.identity, COL_FINISH);
            AddBlock(world + new Vector3(0, 8f, 0),
                     new Vector3(0.5f, 0.5f, TRACK_WIDTH + 6f), Quaternion.identity, COL_FINISH);
            AddBlock(world + new Vector3(0, 4f, -(TRACK_WIDTH * 0.5f + 3f)),
                     new Vector3(0.5f, 8f, 0.5f), Quaternion.identity, COL_BARRIER);
            AddBlock(world + new Vector3(0, 4f, (TRACK_WIDTH * 0.5f + 3f)),
                     new Vector3(0.5f, 8f, 0.5f), Quaternion.identity, COL_BARRIER);
        }

        private void BuildCheckpoint(Vector3 rel)
        {
            Vector3 world = _trackOrigin + rel;

            _checkpointTriggerObj = new GameObject("GolfCartRaceCheckpoint");
            _checkpointTriggerObj.transform.position = world + Vector3.up * 2f;
            _checkpointTriggerObj.transform.rotation = Quaternion.identity;
            _checkpointTriggerObj.transform.localScale = new Vector3(2f, 4f, TRACK_WIDTH);
            _checkpointTriggerObj.AddComponent<BoxCollider>().isTrigger = true;
            _checkpointTriggerObj.AddComponent<CheckpointTrigger>();
            // subscribed in SubscribeTriggers() after GO

            AddBlock(world + new Vector3(0, TRACK_THICKNESS * 0.5f + 0.02f, 0),
                     new Vector3(2f, 0.05f, TRACK_WIDTH), Quaternion.identity, COL_CHECKPOINT);
            AddBlock(world + new Vector3(0, 8f, 0),
                     new Vector3(0.5f, 0.5f, TRACK_WIDTH + 6f), Quaternion.identity, COL_CHECKPOINT);
            AddBlock(world + new Vector3(0, 4f, -(TRACK_WIDTH * 0.5f + 3f)),
                     new Vector3(0.5f, 8f, 0.5f), Quaternion.identity, COL_BARRIER);
            AddBlock(world + new Vector3(0, 4f, (TRACK_WIDTH * 0.5f + 3f)),
                     new Vector3(0.5f, 8f, 0.5f), Quaternion.identity, COL_BARRIER);
        }

        private void AddBlock(Vector3 pos, Vector3 scale, Quaternion rot, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.transform.position = pos;
            go.transform.localScale = scale;
            go.transform.rotation = rot;
            go.layer = LayerMask.NameToLayer("Default");
            var r = go.GetComponent<Renderer>();
            if (r != null)
            {
                r.material = new Material(Shader.Find("Standard"));
                r.material.color = color;
            }
            _trackObjects.Add(go);
        }
        private Vector3 GetGridPosition(int index)
        {
            int col = index % 4;
            int row = index / 4;
            float hw = LOOP_W * 0.5f;
            float hd = LOOP_D * 0.5f;
            float startX = -hw + 20f;
            float z = hd - TRACK_WIDTH * 0.5f + (col + 0.5f) * (TRACK_WIDTH / 4f);
            float x = startX - 4f - row * GRID_SPACING_Z;
            return _trackOrigin + new Vector3(x, TRACK_THICKNESS + 2f, z);
        }

        private void SpawnCarts()
        {
            var prefab = GameManager.GolfCartSettings.Prefab;
            if (prefab == null) return;

            for (int i = 0; i < _racers.Count; i++)
            {
                var player = _racers[i];
                var cart = UnityEngine.Object.Instantiate(prefab, GetGridPosition(i), Quaternion.Euler(0, 90f, 0));
                if (cart == null) continue;

                cart.ServerReserveDriverSeatPreNetworkSpawn(player);
                NetworkServer.Spawn(cart.gameObject);
                cart.ServerReserveDriverSeatPostNetworkSpawn();

                var tracker = cart.AsEntity?.LevelBoundsTracker;
                if (tracker != null)
                {
                    BoundsManager.DeregisterLevelBoundsTracker(tracker);
                    tracker.InformLevelBoundsStateChanged(BoundsState.InBounds);
                }

                try
                {
                    _serverEnter.Invoke(cart, new object[] { player });
                    _raceCarts.Add(cart);
                    GolfMayhemPlugin.Log.LogInfo($"[GolfCartRace] Seated {player.Name} at grid {i}.");
                }
                catch (Exception ex)
                {
                    GolfMayhemPlugin.Log.LogError($"[GolfCartRace] ServerEnter failed for {player.Name}: {ex.Message}");
                }
            }
        }

        private void OnCartCrossedCheckpoint(GolfCartInfo cart)
        {
            if (!RaceActive) return;
            if (cart == null || !cart.TryGetDriver(out PlayerInfo driver)) return;
            if (!_lapCounts.ContainsKey(driver)) return;
            _checkpointPassed.Add(driver);
            GolfMayhemPlugin.Log.LogInfo($"[GolfCartRace] {driver.Name} passed checkpoint.");
        }

        private void OnCartCrossedFinishLine(GolfCartInfo cart)
        {
            if (!RaceActive) return;
            if (cart == null || !cart.TryGetDriver(out PlayerInfo driver)) return;
            if (!_lapCounts.ContainsKey(driver) || _finishOrder.Contains(driver)) return;

            if (!_checkpointPassed.Contains(driver))
            {
                GolfMayhemPlugin.Log.LogInfo($"[GolfCartRace] {driver.Name} crossed finish without checkpoint — ignored.");
                return;
            }

            _checkpointPassed.Remove(driver);
            _lapCounts[driver]++;
            int laps = _lapCounts[driver];
            GolfMayhemPlugin.Log.LogInfo($"[GolfCartRace] {driver.Name} completed lap {laps}.");

            if (laps >= 1)
            {
                _finishOrder.Add(driver);
                GolfMayhemPlugin.Log.LogInfo($"[GolfCartRace] {driver.Name} wins the race!");
                GolfMayhemNetwork.SendWarn(NetworkId, $"{driver.Name} wins!");
            }
        }

        private IEnumerator RaceRoutine()
        {
            yield return new WaitForSeconds(0.1f);

            SpawnCarts();

            yield return new WaitForSeconds(0.1f);

            FreezeAllCarts(true);

            TeeOffCountdown.Show();
            for (float t = 5f; t > 0f; t -= Time.deltaTime)
            {
                TeeOffCountdown.SetRemainingTime(t);
                yield return null;
            }
            TeeOffCountdown.SetRemainingTime(0f);
            yield return new WaitForSeconds(0.1f);
            TeeOffCountdown.Hide();

            FreezeAllCarts(false);
            SubscribeTriggers();
            GolfMayhemPlugin.Log.LogInfo("[GolfCartRace] GO!");

            float elapsed = 0f;
            while (_finishOrder.Count == 0 && elapsed < 300f)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            UnsubscribeTriggers();

            string winner = _finishOrder.Count > 0 ? _finishOrder[0].Name : "Nobody";
            GolfMayhemPlugin.Log.LogInfo($"[GolfCartRace] Race complete! Winner: {winner}");
            yield return new WaitForSeconds(3f);

            GolfMayhemNetwork.SendDeactivate(NetworkId, "Race over! Back to golf.");

            var mgr = ChaosEventManager.Instance;
            if (mgr != null)
                mgr.DeactivateEventLocally(this);
        }

        private void FreezeAllCarts(bool freeze)
        {
            foreach (var cart in _raceCarts)
            {
                if (cart == null) continue;
                var rb = cart.GetComponent<Rigidbody>();
                if (rb == null) continue;

                if (freeze)
                {
                    _origConstraints[cart] = rb.constraints;
                    rb.constraints = RigidbodyConstraints.FreezePosition;
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
                else
                {
                    rb.constraints = _origConstraints.TryGetValue(cart, out var orig)
                        ? orig
                        : RigidbodyConstraints.None;
                }
            }

            if (!freeze) _origConstraints.Clear();
        }

        private void Cleanup()
        {
            foreach (var go in _trackObjects)
                if (go != null) UnityEngine.Object.Destroy(go);
            _trackObjects.Clear();

            if (_finishTriggerObj != null) { UnityEngine.Object.Destroy(_finishTriggerObj); _finishTriggerObj = null; }
            if (_checkpointTriggerObj != null) { UnityEngine.Object.Destroy(_checkpointTriggerObj); _checkpointTriggerObj = null; }

            _checkpointPassed.Clear();

            var localPlayer = GameManager.LocalPlayerInfo;
            if (localPlayer != null)
                BoundsManager.RegisterLevelBoundsTracker(localPlayer.LevelBoundsTracker);

            if (!NetworkServer.active) return;

            foreach (var cart in _raceCarts)
                if (cart != null) NetworkServer.Destroy(cart.gameObject);
            _raceCarts.Clear();
        }

        private bool CacheServerEnter()
        {
            if (_serverEnter != null) return true;
            _serverEnter = typeof(GolfCartInfo).GetMethod(
                "ServerEnter", BindingFlags.Instance | BindingFlags.NonPublic,
                null, new[] { typeof(PlayerInfo) }, null);
            if (_serverEnter == null)
                GolfMayhemPlugin.Log.LogError("[GolfCartRace] Could not find GolfCartInfo.ServerEnter!");
            return _serverEnter != null;
        }
    }

    public class FinishLineTrigger : MonoBehaviour
    {
        public event Action<GolfCartInfo> OnCartCrossed;
        private readonly Dictionary<int, float> _lastCross = new Dictionary<int, float>();
        private const float COOLDOWN = 5f;

        public void ClearCooldowns() => _lastCross.Clear();

        private void OnTriggerEnter(Collider other)
        {
            var cart = other.GetComponentInParent<GolfCartInfo>();
            if (cart == null) return;
            int id = cart.GetInstanceID();
            if (_lastCross.TryGetValue(id, out float last) && Time.time - last < COOLDOWN) return;
            _lastCross[id] = Time.time;
            OnCartCrossed?.Invoke(cart);
        }
    }

    public class CheckpointTrigger : MonoBehaviour
    {
        public event Action<GolfCartInfo> OnCartCrossed;
        private readonly Dictionary<int, float> _lastCross = new Dictionary<int, float>();
        private const float COOLDOWN = 3f;

        public void ClearCooldowns() => _lastCross.Clear();

        private void OnTriggerEnter(Collider other)
        {
            var cart = other.GetComponentInParent<GolfCartInfo>();
            if (cart == null) return;
            int id = cart.GetInstanceID();
            if (_lastCross.TryGetValue(id, out float last) && Time.time - last < COOLDOWN) return;
            _lastCross[id] = Time.time;
            OnCartCrossed?.Invoke(cart);
        }
    }
}