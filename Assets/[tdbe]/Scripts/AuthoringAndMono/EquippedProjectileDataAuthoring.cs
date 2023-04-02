using UnityEngine;
using Unity.Entities;

namespace GameWorld.Pickups
{
    public class EquippedProjectileDataAuthoring : MonoBehaviour
    {
        [Header("This is the pickup data that the Projectile Spawner \nuses to shoot. Obtained from pickups or \nfrom the default 'pickup' on the owner.")]
        public bool active = true;
        [Header("e.g. player.")]
        public GameObject owner;
        [Header("Spawned active visual on owner, e.g. gun.")]
        public GameObject spawnedVisual;
        [Space]
        [Header("This is data for the projectile you shoot:")]
        public GameObject prefab;
        public double timeToLive = 1;
        public float speed = 1;
        public float scale = 1;
        public double pickupTime = 0;
        public double pickupTimeToLive = 1;// alternativly could count shots as time to live
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
                    speed = authoring.speed,
                    scale = authoring.scale,
                    pickupTime = authoring.pickupTime,
                    pickupTimeToLive = authoring.pickupTimeToLive,
                    isCollisionInvulnerable = authoring.isCollisionInvulnerable
                });
            }
        }
    }

    
}