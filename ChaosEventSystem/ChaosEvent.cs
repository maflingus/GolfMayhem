namespace GolfMayhem.ChaosEventSystem
{
    public abstract class ChaosEvent
    {
        public virtual string NetworkId => DisplayName;

        public abstract string DisplayName { get; }

        public abstract string WarnMessage { get; }

        public abstract string ActivateMessage { get; }

        public abstract float Weight { get; }

        public abstract void OnActivate();

        public abstract void OnDeactivate();

        public virtual void OnUpdate() { }

        public virtual bool IsEnabled => true;
    }
}
