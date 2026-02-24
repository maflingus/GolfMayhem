namespace GolfMayhem.ChaosEventSystem
{
    /// <summary>
    /// Base class for all chaos events.
    /// Subclass this and register with GolfMayhemAPI.RegisterEvent() to add custom events.
    /// </summary>
    public abstract class ChaosEvent
    {
        /// <summary>Display name shown in the announcer and logs.</summary>
        public abstract string DisplayName { get; }

        /// <summary>
        /// Short teaser line shown 3 seconds before the event fires.
        /// Example: "⚠️ GRAVITY FLIP INCOMING!"
        /// </summary>
        public abstract string WarnMessage { get; }

        /// <summary>
        /// Full announcement shown when the event activates.
        /// Example: "🌀 GRAVITY FLIP! HOLD ON!"
        /// </summary>
        public abstract string ActivateMessage { get; }

        /// <summary>
        /// Relative probability weight. Higher = chosen more often.
        /// Events with weight 0 are never chosen.
        /// </summary>
        public abstract float Weight { get; }

        /// <summary>
        /// Called when this event becomes active.
        /// Apply your chaos here (physics changes, spawns, etc.)
        /// </summary>
        public abstract void OnActivate();

        /// <summary>
        /// Called when the event duration expires.
        /// Restore everything you changed in OnActivate().
        /// </summary>
        public abstract void OnDeactivate();

        /// <summary>
        /// Optional: called every frame while the event is active.
        /// Useful for per-frame effect updates (e.g., continuous force).
        /// </summary>
        public virtual void OnUpdate() { }

        /// <summary>Whether this event is currently enabled per config.</summary>
        public virtual bool IsEnabled => true;
    }
}
