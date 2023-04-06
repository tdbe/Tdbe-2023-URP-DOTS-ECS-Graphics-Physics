using Unity.Entities;

namespace GameWorld.Pickups
{
    public struct EquippedShieldDataComponent : IComponentData, IEnableableComponent
    {
        public bool active;
        public Entity owner;
        public Entity activeVisual;
        public double pickupTime;
        public double pickupTimeToLive;
    }
}
