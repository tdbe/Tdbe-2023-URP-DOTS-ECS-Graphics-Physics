using Unity.Entities;

namespace GameWorld.Projectiles
{
    public struct ProjectileComponent : IComponentData
    {
        public Entity owner;
        public float timeToLive;
        //isCollisionInvulnerable -- this manifests as a tag
    }
}
