using System.Collections;
using UnityEngine;

namespace GolfMayhem.ChaosEventSystem.Events
{
    public class NightTimeEvent : ChaosEvent
    {
        private const float FADE_DURATION = 2.5f;

        private static readonly Color NIGHT_SKY_COLOR = new Color(0.0f, 0.0f, 0.02f);
        private static readonly Color NIGHT_HORIZON_COLOR = new Color(0.0f, 0.01f, 0.04f);
        private static readonly Vector4 NIGHT_SUN_DIRECTION = new Vector4(0.3f, -0.9f, 0.3f, 0f);
        private const float NIGHT_MOON_CYCLE = 0.5f;
        private const float NIGHT_STARS_EXPOSURE = 50f;
        private const float NIGHT_AMBIENT_INTENSITY = 0f;

        private Material _nightSkyInstance;
        private Material _originalSharedSkybox;
        private Color _originalSkyColor;
        private Color _originalHorizonColor;
        private Vector4 _originalSunDirection;
        private float _originalMoonCycle;
        private float _originalStarsExposure;
        private float _originalAmbientIntensity;
        private Coroutine _fadeCoroutine;

        public override string DisplayName => "Night Time";
        public override string NetworkId => "NightTime";
        public override string WarnMessage => "The sky is darkening...";
        public override string ActivateMessage => "Night falls!";
        public override float Weight => Configuration.WeightNightTime.Value;
        public override bool IsEnabled => Configuration.EnableNightTime.Value && !Patches.RulesPatch.NightTimeEnabled;

        public override void OnActivate()
        {
            _originalSharedSkybox = RenderSettings.skybox;
            if (_originalSharedSkybox == null) return;

            _originalSkyColor = _originalSharedSkybox.GetColor("_SkyColor");
            _originalHorizonColor = _originalSharedSkybox.GetColor("_HorizonColor");
            _originalSunDirection = _originalSharedSkybox.GetVector("_SunDirection");
            _originalMoonCycle = _originalSharedSkybox.GetFloat("_MoonCycle");
            _originalStarsExposure = _originalSharedSkybox.GetFloat("_StarsExposure");
            _originalAmbientIntensity = _originalSharedSkybox.GetFloat("_AmbientIntensity");

            _nightSkyInstance = Object.Instantiate(_originalSharedSkybox);
            RenderSettings.skybox = _nightSkyInstance;

            // Replace cloud noise textures with solid black — removes cloud brightness
            var blackTex = new Texture2D(1, 1, TextureFormat.RGB24, false);
            blackTex.SetPixel(0, 0, Color.black);
            blackTex.Apply();
            _nightSkyInstance.SetTexture("_PerlinTex", blackTex);
            _nightSkyInstance.SetTexture("_VoronoiTex", blackTex);

            // Dark gradient — darkens sky background and tints clouds dark
            var nightGradient = new Texture2D(1, 64, TextureFormat.RGB24, false);
            nightGradient.wrapMode = TextureWrapMode.Clamp;
            var nightPixels = new Color[64];
            for (int i = 0; i < 64; i++)
            {
                float t = i / 63f;
                nightPixels[i] = Color.Lerp(new Color(0.0f, 0.01f, 0.05f), new Color(0f, 0f, 0.01f), t);
            }
            nightGradient.SetPixels(nightPixels);
            nightGradient.Apply();
            _nightSkyInstance.SetTexture("_SkyGradientTex", nightGradient);

            var host = ChaosEventManager.Instance as MonoBehaviour
                    ?? EnvironmentManager.Instance as MonoBehaviour;
            if (host != null)
                _fadeCoroutine = host.StartCoroutine(FadeIn());
        }

        public override void OnDeactivate()
        {
            var host = ChaosEventManager.Instance as MonoBehaviour
                    ?? EnvironmentManager.Instance as MonoBehaviour;
            if (_fadeCoroutine != null && host != null)
                host.StopCoroutine(_fadeCoroutine);
            if (host != null)
                _fadeCoroutine = host.StartCoroutine(FadeOut());
        }

        private IEnumerator FadeIn()
        {
            float elapsed = 0f;
            while (elapsed < FADE_DURATION)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / FADE_DURATION);

                if (_nightSkyInstance != null)
                {
                    _nightSkyInstance.SetColor("_SkyColor", Color.Lerp(_originalSkyColor, NIGHT_SKY_COLOR, t));
                    _nightSkyInstance.SetColor("_HorizonColor", Color.Lerp(_originalHorizonColor, NIGHT_HORIZON_COLOR, t));
                    _nightSkyInstance.SetVector("_SunDirection", Vector4.Lerp(_originalSunDirection, NIGHT_SUN_DIRECTION, t));
                    _nightSkyInstance.SetFloat("_MoonCycle", Mathf.Lerp(_originalMoonCycle, NIGHT_MOON_CYCLE, t));
                    _nightSkyInstance.SetFloat("_StarsExposure", Mathf.Lerp(_originalStarsExposure, NIGHT_STARS_EXPOSURE, t));
                    _nightSkyInstance.SetFloat("_AmbientIntensity", Mathf.Lerp(_originalAmbientIntensity, NIGHT_AMBIENT_INTENSITY, t));
                }
                yield return null;
            }
        }

        private IEnumerator FadeOut()
        {
            float elapsed = 0f;
            while (elapsed < FADE_DURATION)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / FADE_DURATION);

                if (_nightSkyInstance != null)
                {
                    _nightSkyInstance.SetColor("_SkyColor", Color.Lerp(NIGHT_SKY_COLOR, _originalSkyColor, t));
                    _nightSkyInstance.SetColor("_HorizonColor", Color.Lerp(NIGHT_HORIZON_COLOR, _originalHorizonColor, t));
                    _nightSkyInstance.SetVector("_SunDirection", Vector4.Lerp(NIGHT_SUN_DIRECTION, _originalSunDirection, t));
                    _nightSkyInstance.SetFloat("_MoonCycle", Mathf.Lerp(NIGHT_MOON_CYCLE, _originalMoonCycle, t));
                    _nightSkyInstance.SetFloat("_StarsExposure", Mathf.Lerp(NIGHT_STARS_EXPOSURE, _originalStarsExposure, t));
                    _nightSkyInstance.SetFloat("_AmbientIntensity", Mathf.Lerp(NIGHT_AMBIENT_INTENSITY, _originalAmbientIntensity, t));
                }
                yield return null;
            }

            RenderSettings.skybox = _originalSharedSkybox;
            if (_nightSkyInstance != null) { Object.Destroy(_nightSkyInstance); _nightSkyInstance = null; }
            _fadeCoroutine = null;
        }
    }
}