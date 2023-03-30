using UnityEngine;
using Unity.Entities;

namespace GameWorld.Pickups
{
    public class EquippedProjectileDataAuthoring : MonoBehaviour
    {
        [Header("This is the data that the Projectile Spawner will shoot with.\nOverridden by pickups.")]
        public bool active = true;
        public GameObject owner;
        public GameObject activeVisual;
        public GameObject prefab;
        public float timeToLive = 2000;
        public bool isCollisionInvulnerable = false;
        public class EquippedProjectileDataBaker : Baker<EquippedProjectileDataAuthoring>
        {
            public override void Bake(EquippedProjectileDataAuthoring authoring)
            {
                AddComponent<EquippedProjectileDataComponent>(new EquippedProjectileDataComponent{
                    active = authoring.active,
                    owner = GetEntity(authoring.owner),
                    activeVisual = GetEntity(authoring.activeVisual),
                    prefab = GetEntity(authoring.prefab),
                    timeToLive = authoring.timeToLive,
                    isCollisionInvulnerable = authoring.isCollisionInvulnerable
                });
            }
        }
    }

    
}