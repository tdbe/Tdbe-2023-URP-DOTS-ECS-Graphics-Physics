using Unity.Entities;

namespace GameWorld.Pickups
{
    public struct GunPickupTag : IComponentData
    {
        public Entity playerGunPrefab;// player will also have GunPickupPlayerTag for writegroup
    }

    public struct GunPickupPlayerTag : IComponentData
    {
            
    }
}
