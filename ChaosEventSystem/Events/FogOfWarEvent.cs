using UnityEngine;

namespace GolfMayhem.ChaosEventSystem.Events
{
    /// <summary>
    /// CHAOS EVENT: Fog of War
    /// Blankets the entire course in thick atmospheric fog, making it impossible
    /// to see other players' ball positions, the hole flag, or incoming items.
    ///
    /// Uses Unity's built-in RenderSettings.fog for maximum compatibility.
    /// </summary>
    public class FogOfWarEvent : ChaosEvent
    {
        // ── Saved original fog state ──
        private bool   _origFogEnabled;
        private Color  _origFogColor;
        private float  _origFogDensity;
        private FogMode _origFogMode;
        private float  _origFogStart;
        private float  _origFogEnd;

        // ── Chaos fog settings ──
        private static readonly Color FOG_COLOR = new Color(0.15f, 0.15f, 0.15f, 1f); // Dark grey
        private const float FOG_DENSITY = 0.12f;

        public override string DisplayName     => "Fog of War";
        public override string WarnMessage     => "⚠️ A STRANGE MIST APPROACHES...";
        public override string ActivateMessage => "🌫️ FOG OF WAR! YOU CAN'T SEE A THING!";
        public override float  Weight          => Configuration.WeightFogOfWar.Value;
        public override bool   IsEnabled       => Configuration.EnableFogOfWar.Value;

        public override void OnActivate()
        {
            // Save original state so we can perfectly restore it
            _origFogEnabled = RenderSettings.fog;
            _origFogColor   = RenderSettings.fogColor;
            _origFogDensity = RenderSettings.fogDensity;
            _origFogMode    = RenderSettings.fogMode;
            _origFogStart   = RenderSettings.fogStartDistance;
            _origFogEnd     = RenderSettings.fogEndDistance;

            // Apply chaos fog
            RenderSettings.fog          = true;
            RenderSettings.fogMode      = FogMode.Exponential;
            RenderSettings.fogColor     = FOG_COLOR;
            RenderSettings.fogDensity   = FOG_DENSITY;

            GolfMayhemPlugin.Log.LogInfo("[FogOfWar] Fog activated.");
        }

        public override void OnDeactivate()
        {
            // Restore everything exactly as we found it
            RenderSettings.fog              = _origFogEnabled;
            RenderSettings.fogColor         = _origFogColor;
            RenderSettings.fogDensity       = _origFogDensity;
            RenderSettings.fogMode          = _origFogMode;
            RenderSettings.fogStartDistance = _origFogStart;
            RenderSettings.fogEndDistance   = _origFogEnd;

            GolfMayhemPlugin.Log.LogInfo("[FogOfWar] Fog cleared.");
        }
    }
}
