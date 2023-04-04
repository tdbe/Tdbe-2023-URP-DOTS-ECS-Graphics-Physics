using Unity.Entities;

namespace GameWorld.Projectiles
{
    public struct ProjectileComponent : IComponentData
    {
        public Entity owner;
        //public double timeToLive;
        //isCollisionInvulnerable -- this manifests as a tag
    }
}
