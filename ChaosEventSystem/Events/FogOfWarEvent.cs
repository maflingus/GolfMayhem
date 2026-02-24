using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace GolfMayhem.ChaosEventSystem.Events
{
    public class FogOfWarEvent : ChaosEvent
    {
        private const float MAX_ALPHA = 0.65f;
        private const float FADE_IN_TIME = 5.5f;
        private const float FAR_CLIP = 25f;

        private static readonly Color FOG_COLOR = new Color(0.85f, 0.92f, 1f, 1f);

        private GameObject _fogGO;
        private RawImage _fogImage;
        private Camera _camera;
        private float _originalFarClip;

        public override string DisplayName => "Fog of War";
        public override string WarnMessage => "A strange mist approaches...";
        public override string ActivateMessage => "A thick fog has appeared on the map!";
        public override float Weight => Configuration.WeightFogOfWar.Value;
        public override bool IsEnabled => Configuration.EnableFogOfWar.Value;

        public override void OnActivate()
        {
            _camera = GameManager.Camera;
            if (_camera != null)
            {
                _originalFarClip = _camera.farClipPlane;
                _camera.farClipPlane = FAR_CLIP;
            }

            _fogGO = new GameObject("GolfMayhem_FogOverlay");
            Object.DontDestroyOnLoad(_fogGO);

            var canvas = _fogGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 998;

            _fogGO.AddComponent<CanvasScaler>().uiScaleMode =
                CanvasScaler.ScaleMode.ScaleWithScreenSize;

            var imgGO = new GameObject("FogImage");
            imgGO.transform.SetParent(_fogGO.transform, false);

            _fogImage = imgGO.AddComponent<RawImage>();
            _fogImage.texture = BuildRadialGradientTexture(256);
            _fogImage.color = new Color(FOG_COLOR.r, FOG_COLOR.g, FOG_COLOR.b, 0f);

            var rect = _fogImage.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            GolfMayhemPlugin.Instance.StartCoroutine(FadeIn());

            GolfMayhemPlugin.Log.LogInfo("[FogOfWar] Fog overlay spawned.");
        }

        public override void OnDeactivate()
        {
            if (_fogGO != null)
            {
                Object.Destroy(_fogGO);
                _fogGO = null;
                _fogImage = null;
            }

            if (_camera != null)
            {
                _camera.farClipPlane = _originalFarClip;
                _camera = null;
            }

            GolfMayhemPlugin.Log.LogInfo("[FogOfWar] Fog cleared.");
        }

        private IEnumerator FadeIn()
        {
            float elapsed = 0f;
            while (elapsed < FADE_IN_TIME && _fogImage != null)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(0f, MAX_ALPHA, elapsed / FADE_IN_TIME);
                var c = _fogImage.color;
                _fogImage.color = new Color(c.r, c.g, c.b, alpha);
                yield return null;
            }
        }

        private static Texture2D BuildRadialGradientTexture(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, mipChain: false);
            tex.wrapMode = TextureWrapMode.Clamp;

            float center = size * 0.5f;
            float radius = size * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    float t = Mathf.Clamp01(dist / radius);
                    t = t * t;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, t));
                }
            }

            tex.Apply();
            return tex;
        }
    }
}