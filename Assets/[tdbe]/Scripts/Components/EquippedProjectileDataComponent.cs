using Unity.Entities;

namespace GameWorld.Pickups
{
    public struct EquippedProjectileDataComponent : IComponentData
    {
        public bool active;
        public Entity owner;
        public Entity spawnedVisual;
        public Entity prefab;
        public double timeToLive;
        public float speed;
        public float scale;
        public double pickupTime;
        public double pickupTimeToLive;
        public bool isCollisionInvulnerable;
    }
}
