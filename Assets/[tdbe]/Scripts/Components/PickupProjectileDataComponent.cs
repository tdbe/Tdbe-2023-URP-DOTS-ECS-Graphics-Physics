using Unity.Entities;

namespace GameWorld.Pickups
{
    // can be on a pickup or on a player/npc. If it's on a player/npc then it represents the default projectile
    public struct PickupProjectileDataComponent : IComponentData
    {
        public Entity activeVisual;
        public Entity prefab;
        public float timeToLive;
        public bool isCollisionInvulnerable;
    }
}
