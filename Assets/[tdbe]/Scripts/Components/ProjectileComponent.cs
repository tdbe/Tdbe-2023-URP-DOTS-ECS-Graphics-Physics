using Unity.Entities;

namespace GameWorld.Projectiles
{
    public struct ProjectileComponent : IComponentData
    {
        public Entity owner;
        public double spawnTime;
        public float timeToLive;
        // isCollisionInvulnerable as tag
    }
}
