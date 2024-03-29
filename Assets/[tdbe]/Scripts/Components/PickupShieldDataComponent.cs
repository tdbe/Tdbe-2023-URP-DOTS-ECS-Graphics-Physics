using Unity.Entities;

namespace GameWorld.Pickups
{
    // can be on a pickup or on a player/npc. If it's on a player/npc then it represents the owner's default.
    public struct PickupShieldDataComponent : IComponentData
    {
        public bool active;
        public Entity activeVisual;
        //public double pickupTime;
        public double pickupTimeToLive;
    }
}
