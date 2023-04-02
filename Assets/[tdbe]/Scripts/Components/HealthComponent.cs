using Unity.Entities;

namespace GameWorld
{
    public struct HealthComponent : IComponentData
    {
        public double spawnTime;    
        public double timeToLive;
        public float maxHealth;    
        public float currentHealth;// don't modify if owner has invulnerable tag
    }
}
