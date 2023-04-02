using Unity.Entities;

namespace GameWorld
{
    public struct HealthComponent : IComponentData
    {
        public double spawnTime;    
        public double timeToLive;    
        public float health;    
    }
}
