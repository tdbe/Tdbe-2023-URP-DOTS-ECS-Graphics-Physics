using Unity.Entities;

namespace GameWorld.Pickups
{
    public struct EquippedProjectileDataComponent : IComponentData, IEnableableComponent
    {
        public bool active;
        public Entity owner;
        public Entity activeVisual;
        public Entity prefab;
        public double timeToLive;
        public float speed;
        public float scale;
        public double pickupTime;
        public double pickupTimeToLive;
        public bool isCollisionInvulnerable;
    }
}
