using UnityEngine;
using Unity.Entities;

namespace GameWorld.Projectiles
{
    public class ProjectileSysAuthoring : MonoBehaviour
    {

        public class AsteroidPrefabBaker : Baker<ProjectileSysAuthoring>
        {
            public override void Bake(ProjectileSysAuthoring authoring)
            {
                AddComponent<ProjectileSysComponent>(new ProjectileSysComponent{
                });
            }
        }
    }
}