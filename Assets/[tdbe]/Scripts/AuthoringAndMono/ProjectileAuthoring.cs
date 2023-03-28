using UnityEngine;
using Unity.Entities;

namespace GameWorld.Projectiles
{
    public class ProjectileAuthoring : MonoBehaviour
    {
        public GameObject owner;
        public double spawnTime;
        public float timeToLive;
        public class ProjectileBaker : Baker<ProjectileAuthoring>
        {
            public override void Bake(ProjectileAuthoring authoring)
            {
                AddComponent<ProjectileComponent>(new ProjectileComponent{
                   owner = GetEntity(authoring.owner),
                   spawnTime = authoring.spawnTime,
                   timeToLive = authoring.timeToLive
                });
            }
        }
    }

    
}