using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GolfMayhem.ChaosEventSystem.Events
{

    public class NightTimeEvent : ChaosEvent
    {
        private const float FADE_DURATION = 2f;
        private const float NIGHT_LIGHT_INTENSITY = 0.02f;

        private readonly Dictionary<Light, float> _originalLightIntensities = new Dictionary<Light, float>();
        private Material _skyboxMaterial;
        private Color _originalSkyboxTint;
        private bool _skyboxHasTint;
        private float _originalAmbientIntensity;
        private Coroutine _fadeCoroutine;

        public override string DisplayName => "Night Time";
        public override string NetworkId => "NightTime";
        public override string WarnMessage => "The sky is darkening...";
        public override string ActivateMessage => "Night has fallen, watch your step!";
        public override float Weight => Configuration.WeightNightTime.Value;
        public override bool IsEnabled => Configuration.EnableNightTime.Value;

        public override void OnActivate()
        {
            _originalLightIntensities.Clear();

            foreach (var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (light.type == LightType.Directional)
                    _originalLightIntensities[light] = light.intensity;

            _originalAmbientIntensity = RenderSettings.ambientIntensity;

            _skyboxMaterial = RenderSettings.skybox;
            _skyboxHasTint = false;
            _originalSkyboxTint = Color.white;
            if (_skyboxMaterial != null)
            {
                if (_skyboxMaterial.HasProperty("_Tint"))
                {
                    _originalSkyboxTint = _skyboxMaterial.GetColor("_Tint");
                    _skyboxHasTint = true;
                }
                else if (_skyboxMaterial.HasProperty("_SkyTint"))
                {
                    _originalSkyboxTint = _skyboxMaterial.GetColor("_SkyTint");
                    _skyboxHasTint = true;
                }
            }

            var host = ChaosEventManager.Instance;
            if (host != null)
                _fadeCoroutine = host.StartCoroutine(FadeIn());

            GolfMayhemPlugin.Log.LogInfo($"[NightTime] Activated. Lights: {_originalLightIntensities.Count}, skybox tint: {_skyboxHasTint}, material: {(_skyboxMaterial != null ? _skyboxMaterial.shader.name : "null")}");
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
            var startIntensities = new Dictionary<Light, float>();
            foreach (var kvp in _originalLightIntensities)
                if (kvp.Key != null) startIntensities[kvp.Key] = kvp.Key.intensity;
            float startAmbient = RenderSettings.ambientIntensity;
            Color startTint = _skyboxHasTint ? GetSkyboxTint() : Color.white;

            while (elapsed < FADE_DURATION)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / FADE_DURATION);

                foreach (var kvp in _originalLightIntensities)
                    if (kvp.Key != null)
                        kvp.Key.intensity = Mathf.Lerp(startIntensities[kvp.Key], NIGHT_LIGHT_INTENSITY, t);

                RenderSettings.ambientIntensity = Mathf.Lerp(startAmbient, 0.05f, t);

                if (_skyboxHasTint)
                    SetSkyboxTint(Color.Lerp(startTint, new Color(0.05f, 0.05f, 0.15f), t));

                yield return null;
            }

            foreach (var kvp in _originalLightIntensities)
                if (kvp.Key != null) kvp.Key.intensity = NIGHT_LIGHT_INTENSITY;
            RenderSettings.ambientIntensity = 0.05f;
            if (_skyboxHasTint) SetSkyboxTint(new Color(0.05f, 0.05f, 0.15f));
        }

        private IEnumerator FadeOut()
        {
            float elapsed = 0f;
            var startIntensities = new Dictionary<Light, float>();
            foreach (var kvp in _originalLightIntensities)
                if (kvp.Key != null) startIntensities[kvp.Key] = kvp.Key.intensity;
            float startAmbient = RenderSettings.ambientIntensity;
            Color startTint = _skyboxHasTint ? GetSkyboxTint() : Color.white;

            while (elapsed < FADE_DURATION)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / FADE_DURATION);

                foreach (var kvp in _originalLightIntensities)
                    if (kvp.Key != null)
                        kvp.Key.intensity = Mathf.Lerp(startIntensities[kvp.Key], kvp.Value, t);

                RenderSettings.ambientIntensity = Mathf.Lerp(startAmbient, _originalAmbientIntensity, t);

                if (_skyboxHasTint)
                    SetSkyboxTint(Color.Lerp(startTint, _originalSkyboxTint, t));

                yield return null;
            }

            foreach (var kvp in _originalLightIntensities)
                if (kvp.Key != null) kvp.Key.intensity = kvp.Value;
            RenderSettings.ambientIntensity = _originalAmbientIntensity;
            if (_skyboxHasTint) SetSkyboxTint(_originalSkyboxTint);

            _fadeCoroutine = null;
            _originalLightIntensities.Clear();
        }

        private Color GetSkyboxTint()
        {
            if (_skyboxMaterial == null) return Color.white;
            if (_skyboxMaterial.HasProperty("_Tint")) return _skyboxMaterial.GetColor("_Tint");
            if (_skyboxMaterial.HasProperty("_SkyTint")) return _skyboxMaterial.GetColor("_SkyTint");
            return Color.white;
        }

        private void SetSkyboxTint(Color color)
        {
            if (_skyboxMaterial == null) return;
            if (_skyboxMaterial.HasProperty("_Tint")) _skyboxMaterial.SetColor("_Tint", color);
            if (_skyboxMaterial.HasProperty("_SkyTint")) _skyboxMaterial.SetColor("_SkyTint", color);
        }
    }
}