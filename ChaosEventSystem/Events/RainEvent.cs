using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GolfMayhem.ChaosEventSystem.Events
{
    public class RainEvent : ChaosEvent
    {
        public override string DisplayName => "Rain";
        public override string NetworkId => "Rain";
        public override string WarnMessage => "Storm incoming...";
        public override string ActivateMessage => "It's raining!";
        public override float Weight => Configuration.WeightRain.Value;
        public override bool IsEnabled => Configuration.EnableRain.Value && !Patches.RulesPatch.RainEnabled;
        public override float Duration => Configuration.ChaosEventDuration.Value;

        private readonly List<GameObject> _rainSystems = new List<GameObject>();
        private Coroutine _followCoroutine;
        private float _origAmbient;
        private float _origFogDensity;
        private bool _origFog;
        private Color _origFogColor;
        private FMOD.Studio.EventInstance _rainAudioInstance;


        public override void OnActivate()
        {
            _rainSystems.Clear();

            var allPlayers = new List<PlayerInfo>();
            if (GameManager.LocalPlayerInfo != null)
                allPlayers.Add(GameManager.LocalPlayerInfo);
            foreach (var p in new List<PlayerInfo>(GameManager.RemotePlayers))
                if (p != null) allPlayers.Add(p);

            foreach (var player in allPlayers)
            {
               
                _rainSystems.Add(CreateRainSystem(player.transform, heavy: true));
                _rainSystems.Add(CreateRainSystem(player.transform, heavy: false));
            }

            _origAmbient = RenderSettings.ambientIntensity;
            _origFogDensity = RenderSettings.fogDensity;
            _origFog = RenderSettings.fog;
            _origFogColor = RenderSettings.fogColor;

            RenderSettings.ambientIntensity = _origAmbient * 0.55f;
            RenderSettings.fogColor = new Color(0.5f, 0.55f, 0.65f);
            RenderSettings.fogDensity = _origFogDensity + 0.004f;
            RenderSettings.fog = true;

            var host = ChaosEventManager.Instance as MonoBehaviour
                    ?? EnvironmentManager.Instance as MonoBehaviour;

            _followCoroutine = host?.StartCoroutine(FollowPlayersRoutine(allPlayers));

            _rainAudioInstance = FMODUnity.RuntimeManager.CreateInstance(GameManager.AudioSettings.FoliageLoopEvent);
            _rainAudioInstance.setVolume(0.6f);
            _rainAudioInstance.start();
            _rainAudioInstance.release();
        }

        public override void OnDeactivate()
        {
            var host = ChaosEventManager.Instance as MonoBehaviour
                    ?? EnvironmentManager.Instance as MonoBehaviour;
            if (_followCoroutine != null) host?.StopCoroutine(_followCoroutine);

            foreach (var go in _rainSystems) if (go != null) Object.Destroy(go);
            _rainSystems.Clear();

            if (_rainAudioInstance.isValid())
                _rainAudioInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);

            RenderSettings.ambientIntensity = _origAmbient;
            RenderSettings.fogDensity = _origFogDensity;
            RenderSettings.fog = _origFog;
            RenderSettings.fogColor = _origFogColor;
        }
        private IEnumerator FollowPlayersRoutine(List<PlayerInfo> players)
        {
            while (true)
            {
                for (int i = 0; i < players.Count; i++)
                {
                    if (players[i] == null) continue;
                    var pos = players[i].transform.position;
                    int baseIdx = i * 2;
                    if (baseIdx < _rainSystems.Count && _rainSystems[baseIdx] != null)
                        _rainSystems[baseIdx].transform.position = new Vector3(pos.x, pos.y + 22f, pos.z);
                    if (baseIdx + 1 < _rainSystems.Count && _rainSystems[baseIdx + 1] != null)
                        _rainSystems[baseIdx + 1].transform.position = new Vector3(pos.x, pos.y + 28f, pos.z);
                }
                yield return null;
            }
        }

        private static GameObject CreateRainSystem(Transform player, bool heavy)
        {
            var go = new GameObject(heavy ? "GolfMayhem_Rain_Heavy" : "GolfMayhem_Rain_Mist");
            Object.DontDestroyOnLoad(go);
            go.transform.position = player.position + Vector3.up * (heavy ? 22f : 28f);

            var ps = go.AddComponent<ParticleSystem>();
            var renderer = go.GetComponent<ParticleSystemRenderer>();

            var main = ps.main;
            main.loop = true;
            main.playOnAwake = true;
            main.duration = 1f;
            main.startLifetime = heavy
                ? new ParticleSystem.MinMaxCurve(0.8f, 1.1f)
                : new ParticleSystem.MinMaxCurve(1.5f, 2.2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0f, 0f);
            main.startSize = heavy
                ? new ParticleSystem.MinMaxCurve(0.05f, 0.1f)
                : new ParticleSystem.MinMaxCurve(0.02f, 0.04f);
            main.startColor = heavy
                ? new ParticleSystem.MinMaxGradient(
                    new Color(0.78f, 0.88f, 1.0f, 0.65f),
                    new Color(0.68f, 0.80f, 1.0f, 0.45f))
                : new ParticleSystem.MinMaxGradient(
                    new Color(0.8f, 0.88f, 1.0f, 0.25f),
                    new Color(0.7f, 0.82f, 1.0f, 0.15f));
            main.gravityModifier = 0f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = heavy ? 600 : 1000;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = heavy ? 250f : 500f;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = heavy
                ? new Vector3(25f, 0.1f, 25f)
                : new Vector3(35f, 0.1f, 35f);

            var velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(-4f, -2f); // consistent wind direction
            velocity.y = heavy
                ? new ParticleSystem.MinMaxCurve(-26f, -20f)
                : new ParticleSystem.MinMaxCurve(-10f, -6f);
            velocity.z = new ParticleSystem.MinMaxCurve(-1f, 1f);

            var sizeOverLife = ps.sizeOverLifetime;
            sizeOverLife.enabled = true;
            var sizeCurve = new AnimationCurve();
            sizeCurve.AddKey(0f, 0.6f);
            sizeCurve.AddKey(1f, 1.0f);
            sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            if (heavy)
            {
                var subGo = new GameObject("GolfMayhem_Splash");
                subGo.transform.SetParent(go.transform);
                var subPs = subGo.AddComponent<ParticleSystem>();

                var subMain = subPs.main;
                subMain.loop = false;
                subMain.playOnAwake = false;
                subMain.startLifetime = new ParticleSystem.MinMaxCurve(0.15f, 0.3f);
                subMain.startSpeed = new ParticleSystem.MinMaxCurve(1f, 3f);
                subMain.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
                subMain.startColor = new ParticleSystem.MinMaxGradient(
                    new Color(0.8f, 0.9f, 1.0f, 0.7f),
                    new Color(0.7f, 0.85f, 1.0f, 0.4f));
                subMain.gravityModifier = 0.4f;
                subMain.simulationSpace = ParticleSystemSimulationSpace.World;
                subMain.maxParticles = 8;

                var subEmission = subPs.emission;
                subEmission.enabled = true;
                subEmission.rateOverTime = 0f;
                subEmission.SetBursts(new[] { new ParticleSystem.Burst(0f, 3, 6) });

                var subShape = subPs.shape;
                subShape.enabled = true;
                subShape.shapeType = ParticleSystemShapeType.Sphere;
                subShape.radius = 0.05f;

                var subRenderer = subGo.GetComponent<ParticleSystemRenderer>();
                var subMat = new Material(Shader.Find("Particles/Standard Unlit")
                                       ?? Shader.Find("Legacy Shaders/Particles/Additive")
                                       ?? Shader.Find("Sprites/Default"));
                subMat.color = new Color(0.8f, 0.9f, 1.0f, 0.6f);
                subRenderer.material = subMat;

                var subEmitters = ps.subEmitters;
                subEmitters.enabled = true;
                subEmitters.AddSubEmitter(subPs, ParticleSystemSubEmitterType.Death, ParticleSystemSubEmitterProperties.InheritNothing);
            }

            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.velocityScale = heavy ? 0.06f : 0.03f;
            renderer.lengthScale = heavy ? 2.5f : 1.5f;
            renderer.sortingFudge = -10f;

            var shader = Shader.Find("Particles/Standard Unlit")
                      ?? Shader.Find("Legacy Shaders/Particles/Additive")
                      ?? Shader.Find("Sprites/Default");
            var mat = new Material(shader);
            mat.color = heavy
                ? new Color(0.78f, 0.88f, 1.0f, 0.55f)
                : new Color(0.82f, 0.90f, 1.0f, 0.25f);
            renderer.material = mat;

            ps.Play();

            if (heavy)
            {
                var watcher = go.AddComponent<RainSplashWatcher>();
                watcher.Init(ps);
            }

            return go;
        }
    }

    public class RainSplashWatcher : MonoBehaviour
    {
        private ParticleSystem _ps;
        private ParticleSystem.Particle[] _particles;
        private int _prevCount;
        private float _splashCooldown;
        private const float SPLASH_INTERVAL = 0.08f; // throttle so we don't spam VFX pool

        public void Init(ParticleSystem ps)
        {
            _ps = ps;
            _particles = new ParticleSystem.Particle[ps.main.maxParticles];
        }

        private void Update()
        {
            if (_ps == null) return;

            _splashCooldown -= Time.deltaTime;
            if (_splashCooldown > 0f) return;

            int count = _ps.GetParticles(_particles);

            if (_splashCooldown <= 0f && count > 0)
            {
                // Pick a random subset of live particles to splash under
                int splashCount = Mathf.Min(3, count);
                for (int i = 0; i < splashCount; i++)
                {
                    int idx = UnityEngine.Random.Range(0, count);
                    var pos = _particles[idx].position;

                    // Raycast down to find the actual ground surface
                    if (Physics.Raycast(pos, Vector3.down, out RaycastHit hit, 50f))
                    {
                        VfxManager.PlayPooledVfxLocalOnly(
                            VfxType.WaterImpactSmall,
                            hit.point,
                            Quaternion.identity);
                    }
                }
                _splashCooldown = SPLASH_INTERVAL;
            }

            _prevCount = count;
        }
    }
}