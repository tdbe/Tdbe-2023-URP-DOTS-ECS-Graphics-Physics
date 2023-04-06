using UnityEngine;
using Unity.Entities;

namespace GameWorld.Projectiles
{
    public class ProjectileAuthoring : MonoBehaviour
    {
        [TextArea(1,3)]
        public string info = "Stores owner (whom it's shot from).";
        [HideInInspector]
        public GameObject owner;
        //[HideInInspector]
        //public double timeToLive = 1;
        public class Projectileaker : Baker<ProjectileAuthoring>
        {
            public override void Bake(ProjectileAuthoring authoring)
            {
                AddComponent<ProjectileComponent>(new ProjectileComponent{
                    owner = GetEntity(authoring.owner)
                    //timeToLive = authoring.timeToLive
                });
            }
        }
    }

    
}