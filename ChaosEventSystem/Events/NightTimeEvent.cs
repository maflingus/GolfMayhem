using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GolfMayhem.ChaosEventSystem.Events
{
    public class NightTimeEvent : ChaosEvent
    {
        private const float FADE_DURATION = 2.5f;
        private const float NIGHT_LIGHT_INTENSITY = 0.05f;

        private static readonly Color NIGHT_SKY_COLOR = new Color(0.0f, 0.0f, 0.02f);
        private static readonly Color NIGHT_HORIZON_COLOR = new Color(0.0f, 0.01f, 0.04f);
        private static readonly Vector4 NIGHT_SUN_DIRECTION = new Vector4(0.3f, -0.9f, 0.3f, 0f);
        private const float NIGHT_MOON_CYCLE = 0.5f;
        private const float NIGHT_STARS_EXPOSURE = 5.0f;

        private readonly Dictionary<Light, float> _originalLightIntensities = new Dictionary<Light, float>();
        private Material _nightSkyInstance;
        private Material _originalSharedSkybox;
        private Color _originalSkyColor;
        private Color _originalHorizonColor;
        private Vector4 _originalSunDirection;
        private float _originalMoonCycle;
        private float _originalStarsExposure;
        private Texture _originalGradientTex;
        private Coroutine _fadeCoroutine;

        public override string DisplayName => "Night Time";
        public override string NetworkId => "NightTime";
        public override string WarnMessage => "Dusk is approaching...";
        public override string ActivateMessage => "Night has fallen over the golf course!";
        public override float Weight => Configuration.WeightNightTime.Value;
        public override bool IsEnabled => Configuration.EnableNightTime.Value;

        public override void OnActivate()
        {
            _originalLightIntensities.Clear();
            foreach (var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (light.type == LightType.Directional)
                    _originalLightIntensities[light] = light.intensity;

            _originalSharedSkybox = RenderSettings.skybox;
            if (_originalSharedSkybox != null)
            {
                _originalSkyColor = _originalSharedSkybox.GetColor("_SkyColor");
                _originalHorizonColor = _originalSharedSkybox.GetColor("_HorizonColor");
                _originalSunDirection = _originalSharedSkybox.GetVector("_SunDirection");
                _originalMoonCycle = _originalSharedSkybox.GetFloat("_MoonCycle");
                _originalStarsExposure = _originalSharedSkybox.GetFloat("_StarsExposure");
                _originalGradientTex = _originalSharedSkybox.GetTexture("_SkyGradientTex");

                _nightSkyInstance = Object.Instantiate(_originalSharedSkybox);
                RenderSettings.skybox = _nightSkyInstance;

                // Create a very dark blue gradient to replace the daytime sky gradient
                // Top = near black, bottom = very dark navy, matching a night sky
                var nightGradient = new Texture2D(1, 64, TextureFormat.RGB24, false);
                nightGradient.wrapMode = TextureWrapMode.Clamp;
                var nightPixels = new Color[64];
                for (int i = 0; i < 64; i++)
                {
                    float t = i / 63f;
                    // Bottom (t=0): very dark navy, Top (t=1): near black
                    nightPixels[i] = Color.Lerp(new Color(0.0f, 0.01f, 0.05f), new Color(0f, 0f, 0.01f), t);
                }
                nightGradient.SetPixels(nightPixels);
                nightGradient.Apply();
                _nightSkyInstance.SetTexture("_SkyGradientTex", nightGradient);
            }

            var host = ChaosEventManager.Instance;
            if (host != null)
                _fadeCoroutine = host.StartCoroutine(FadeIn());

            GolfMayhemPlugin.Log.LogInfo($"[NightTime] Activated. Lights: {_originalLightIntensities.Count}");
        }

        public override void OnDeactivate()
        {
            var host = ChaosEventManager.Instance;
            if (_fadeCoroutine != null && host != null)
                host.StopCoroutine(_fadeCoroutine);
            if (host != null)
                _fadeCoroutine = host.StartCoroutine(FadeOut());
        }

        private IEnumerator FadeIn()
        {
            float elapsed = 0f;
            var startLights = SnapshotLights();

            while (elapsed < FADE_DURATION)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / FADE_DURATION);

                foreach (var kvp in _originalLightIntensities)
                    if (kvp.Key != null)
                        kvp.Key.intensity = Mathf.Lerp(startLights[kvp.Key], NIGHT_LIGHT_INTENSITY, t);

                if (_nightSkyInstance != null)
                {
                    _nightSkyInstance.SetColor("_SkyColor", Color.Lerp(_originalSkyColor, NIGHT_SKY_COLOR, t));
                    _nightSkyInstance.SetColor("_HorizonColor", Color.Lerp(_originalHorizonColor, NIGHT_HORIZON_COLOR, t));
                    _nightSkyInstance.SetVector("_SunDirection", Vector4.Lerp(_originalSunDirection, NIGHT_SUN_DIRECTION, t));
                    _nightSkyInstance.SetFloat("_MoonCycle", Mathf.Lerp(_originalMoonCycle, NIGHT_MOON_CYCLE, t));
                    _nightSkyInstance.SetFloat("_StarsExposure", Mathf.Lerp(_originalStarsExposure, NIGHT_STARS_EXPOSURE, t));
                }

                yield return null;
            }
        }

        private IEnumerator FadeOut()
        {
            float elapsed = 0f;
            var startLights = SnapshotLights();

            while (elapsed < FADE_DURATION)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / FADE_DURATION);

                foreach (var kvp in _originalLightIntensities)
                    if (kvp.Key != null)
                        kvp.Key.intensity = Mathf.Lerp(startLights[kvp.Key], kvp.Value, t);

                if (_nightSkyInstance != null)
                {
                    _nightSkyInstance.SetColor("_SkyColor", Color.Lerp(NIGHT_SKY_COLOR, _originalSkyColor, t));
                    _nightSkyInstance.SetColor("_HorizonColor", Color.Lerp(NIGHT_HORIZON_COLOR, _originalHorizonColor, t));
                    _nightSkyInstance.SetVector("_SunDirection", Vector4.Lerp(NIGHT_SUN_DIRECTION, _originalSunDirection, t));
                    _nightSkyInstance.SetFloat("_MoonCycle", Mathf.Lerp(NIGHT_MOON_CYCLE, _originalMoonCycle, t));
                    _nightSkyInstance.SetFloat("_StarsExposure", Mathf.Lerp(NIGHT_STARS_EXPOSURE, _originalStarsExposure, t));
                }

                yield return null;
            }

            // Restore original shared skybox and destroy our instance
            RenderSettings.skybox = _originalSharedSkybox;
            if (_nightSkyInstance != null)
            {
                Object.Destroy(_nightSkyInstance);
                _nightSkyInstance = null;
            }

            foreach (var kvp in _originalLightIntensities)
                if (kvp.Key != null) kvp.Key.intensity = kvp.Value;

            _fadeCoroutine = null;
            _originalLightIntensities.Clear();
        }

        private Dictionary<Light, float> SnapshotLights()
        {
            var snap = new Dictionary<Light, float>();
            foreach (var kvp in _originalLightIntensities)
                if (kvp.Key != null) snap[kvp.Key] = kvp.Key.intensity;
            return snap;
        }
    }
}