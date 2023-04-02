using Unity.Entities;

namespace GameWorld.Pickups
{
    public struct EquippedShieldDataComponent : IComponentData
    {
        public bool active;
        public Entity owner;
        public Entity spawnedVisual;
        public double pickupTime;
        public double pickupTimeToLive;
    }
}
