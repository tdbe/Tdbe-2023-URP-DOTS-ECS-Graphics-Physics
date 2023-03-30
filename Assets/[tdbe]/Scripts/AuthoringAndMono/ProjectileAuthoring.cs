using UnityEngine;
using Unity.Entities;

namespace GameWorld.Projectiles
{
    public class ProjectileAuthoring : MonoBehaviour
    {
        [HideInInspector]
        public GameObject owner;
        [HideInInspector]
        public float timeToLive = 2000;
        public class Projectileaker : Baker<ProjectileAuthoring>
        {
            public override void Bake(ProjectileAuthoring authoring)
            {
                AddComponent<ProjectileComponent>(new ProjectileComponent{
                    owner = GetEntity(authoring.owner),
                    timeToLive = authoring.timeToLive
                });
            }
        }
    }

    
}