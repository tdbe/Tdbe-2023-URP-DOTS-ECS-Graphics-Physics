using Unity.Entities;

namespace GameWorld.Projectiles
{
    public struct GunComponent : IComponentData
    {
        public float projectileTimeToLive;
        public bool projectileIsCollisionInvulnerable;
    }
}
