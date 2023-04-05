using UnityEngine;
using Unity.Entities;

namespace GameWorld
{
    public class HealthAuthoring : MonoBehaviour
    {
        [Header("Stores health, spawn time, time to live. Can be overridden by systems.")]
        public float maxHealth = 1;
        public float currentHealth = 1;
        [Header("TTL -1 means forever.")]
        public double timeToLive = -1;
        
        public class HealthBaker : Baker<HealthAuthoring>
        {
            public override void Bake(HealthAuthoring authoring)
            {
                AddComponent<HealthComponent>(new HealthComponent{
                    maxHealth = authoring.maxHealth,
                    currentHealth = authoring.currentHealth,
                    timeToLive = authoring.timeToLive
                });
            }
        }
    }

    
}