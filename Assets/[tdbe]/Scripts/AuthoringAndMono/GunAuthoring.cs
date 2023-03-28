using UnityEngine;
using Unity.Entities;

namespace GameWorld.Projectiles
{
    public class GunAuthoring : MonoBehaviour
    {
        public float projectileTimeToLive = 2000;
        public bool projectileIsCollisionInvulnerable = false;
        public class GunBaker : Baker<GunAuthoring>
        {
            public override void Bake(GunAuthoring authoring)
            {
                AddComponent<GunComponent>(new GunComponent{
                   projectileTimeToLive = authoring.projectileTimeToLive,
                   projectileIsCollisionInvulnerable = authoring.projectileIsCollisionInvulnerable
                });
            }
        }
    }

    
}