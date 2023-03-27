using Unity.Entities;

namespace GameWorld.Pickups
{
    public struct ShieldPickupTag : IComponentData
    {
        public Entity playerShieldPrefab;// player will also have ShieldPickupPlayerTag for writegroup
    }

    public struct ShieldPickupPlayerTag : IComponentData
    {
            
    }
}
