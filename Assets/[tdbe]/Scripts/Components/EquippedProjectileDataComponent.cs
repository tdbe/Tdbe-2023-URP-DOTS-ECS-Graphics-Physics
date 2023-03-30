using Unity.Entities;

namespace GameWorld.Pickups
{
    public struct EquippedProjectileDataComponent : IComponentData
    {
        public bool active;
        public Entity owner;
        public Entity activeVisual;
        public Entity prefab;
        public float timeToLive;
        public bool isCollisionInvulnerable;
    }
}
