using UnityEngine;
using Unity.Entities;

namespace GameWorld.Pickups
{
    public class EquippedProjectileDataAuthoring : MonoBehaviour
    {
        [Header("This is the pickup data that the Projectile Spawner \nuses to shoot Taken from pickups or the default 'pickup' on the owner.")]
        public bool active = true;
        [Header("e.g. player.")]
        public GameObject owner;
        [Header("Spawned active visual on owner, e.g. gun.")]
        public GameObject spawnedVisual;
        [Space]
        [Header("This is data for the projectile you shoot:")]
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
                    spawnedVisual = GetEntity(authoring.spawnedVisual),
                    prefab = GetEntity(authoring.prefab),
                    timeToLive = authoring.timeToLive,
                    isCollisionInvulnerable = authoring.isCollisionInvulnerable
                });
            }
        }
    }

    
}