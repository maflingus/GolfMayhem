using UnityEngine;

namespace GolfMayhem.ChaosEventSystem.Events
{

    public class CoffeeEvent : ChaosEvent
    {
        public override string DisplayName => "Coffee Rush";
        public override string NetworkId => "CoffeeRush";
        public override string WarnMessage => "Something's brewing...";
        public override string ActivateMessage => "Coffee rush! Everyone's wired!";
        public override float Weight => Configuration.WeightCoffeeRush.Value;
        public override bool IsEnabled => Configuration.EnableCoffeeRush.Value;

        public override void OnActivate()
        {
            var local = GameManager.LocalPlayerInfo;
            if (local?.Movement == null) return;

            local.Movement.InformDrankCoffee();
            GolfMayhemPlugin.Log.LogInfo("[CoffeeRush] Speed boost applied to local player.");
        }

        public override void OnDeactivate()
        {
            GolfMayhemPlugin.Log.LogInfo("[CoffeeRush] Deactivated.");
        }
    }
}