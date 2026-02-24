using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace GolfMayhem.HUD
{
    /// <summary>
    /// Renders animated announcement text at the top-center of the screen.
    /// Used by ChaosEventManager and game event patches to call out big moments.
    ///
    /// Creates its own Canvas so it's completely independent of the game's UI.
    /// </summary>
    public class AnnouncerHUD : MonoBehaviour
    {
        public static AnnouncerHUD Instance { get; private set; }

        private Canvas    _canvas;
        private Text      _mainText;
        private Text      _shadowText; // Drop shadow for readability
        private Coroutine _displayCoroutine;

        // ─────────────────────────────────────────────────────────────
        // Unity lifecycle
        // ─────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            BuildHUD();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ─────────────────────────────────────────────────────────────
        // Public API
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Display an announcement message on screen.
        /// Safe to call from any thread (runs on main thread via coroutine).
        /// </summary>
        public void ShowAnnouncement(string message, Color color)
        {
            if (_mainText == null) return;

            if (_displayCoroutine != null)
                StopCoroutine(_displayCoroutine);

            _displayCoroutine = StartCoroutine(DisplayRoutine(message, color));
        }

        // ─────────────────────────────────────────────────────────────
        // HUD Construction
        // ─────────────────────────────────────────────────────────────

        private void BuildHUD()
        {
            // ── Canvas ────────────────────────────────────────────────
            var canvasGO = new GameObject("GolfMayhem_AnnouncerCanvas");
            canvasGO.transform.SetParent(transform);

            _canvas = canvasGO.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 999; // Always on top

            canvasGO.AddComponent<CanvasScaler>().uiScaleMode =
                CanvasScaler.ScaleMode.ScaleWithScreenSize;

            // ── Shadow text (renders slightly offset, black) ──────────
            _shadowText = CreateTextElement(canvasGO.transform, "Shadow");
            _shadowText.color = new Color(0f, 0f, 0f, 0.8f);
            var shadowRect = _shadowText.GetComponent<RectTransform>();
            PositionAnnouncer(shadowRect, offsetY: -3f);

            // ── Main text ─────────────────────────────────────────────
            _mainText = CreateTextElement(canvasGO.transform, "Main");
            _mainText.color = Color.white;
            var mainRect = _mainText.GetComponent<RectTransform>();
            PositionAnnouncer(mainRect, offsetY: 0f);

            // Start hidden
            SetAlpha(0f);

            GolfMayhemPlugin.Log.LogInfo("AnnouncerHUD canvas built.");
        }

        private Text CreateTextElement(Transform parent, string name)
        {
            var go = new GameObject($"AnnouncerText_{name}");
            go.transform.SetParent(parent, false);

            var text = go.AddComponent<Text>();
            text.font = Font.CreateDynamicFontFromOSFont("Arial", 48);
            text.fontSize = (int)Configuration.AnnouncerFontSize.Value;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            return text;
        }

        private static void PositionAnnouncer(RectTransform rect, float offsetY)
        {
            // Anchor to top-center
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot     = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -80f + offsetY);
            rect.sizeDelta = new Vector2(1200f, 100f);
        }

        // ─────────────────────────────────────────────────────────────
        // Display coroutine (fade in → hold → fade out)
        // ─────────────────────────────────────────────────────────────

        private IEnumerator DisplayRoutine(string message, Color mainColor)
        {
            float displayTime = Configuration.AnnouncerDisplayTime.Value;

            _mainText.text   = message;
            _shadowText.text = message;
            _mainText.color  = mainColor;

            // Fade in (0.25s)
            yield return Fade(0f, 1f, 0.25f);

            // Hold
            yield return new WaitForSeconds(displayTime);

            // Fade out (0.5s)
            yield return Fade(1f, 0f, 0.5f);
        }

        private IEnumerator Fade(float fromAlpha, float toAlpha, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                SetAlpha(Mathf.Lerp(fromAlpha, toAlpha, elapsed / duration));
                yield return null;
            }
            SetAlpha(toAlpha);
        }

        private void SetAlpha(float alpha)
        {
            if (_mainText != null)
            {
                var c = _mainText.color;
                _mainText.color = new Color(c.r, c.g, c.b, alpha);
            }
            if (_shadowText != null)
            {
                var c = _shadowText.color;
                _shadowText.color = new Color(c.r, c.g, c.b, alpha * 0.8f);
            }
        }
    }
}
