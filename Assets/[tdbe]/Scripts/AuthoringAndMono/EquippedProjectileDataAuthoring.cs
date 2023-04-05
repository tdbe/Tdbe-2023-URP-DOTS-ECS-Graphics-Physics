using UnityEngine;
using Unity.Entities;

namespace GameWorld.Pickups
{
    public class EquippedProjectileDataAuthoring : MonoBehaviour
    {
        [Header("This is the equipment slot for the pickup data. \nThe Projectile Spawner uses this to shoot. \nStores the data from pickups or \nfrom a default 'pickup' on the owner if available.")]
        public bool active = true;
        [Header("Owner, e.g. player.")]
        public GameObject owner;
        [HideInInspector]
        [Header("Everything below here is automatically fetched from the pickup. \nNormally here I would hide this or have it as a component reference ")]
        public GameObject spawnedVisual;
        [Space]
        [HideInInspector]
        public GameObject prefab;
        [HideInInspector]
        public double timeToLive = 1;
        [HideInInspector]
        public float speed = 1;
        [HideInInspector]
        public float scale = 1;
        [HideInInspector]
        public double pickupTime = 0;
        [HideInInspector]
        public double pickupTimeToLive = 0;// alternativly could count shots as time to live
        [HideInInspector]
        public bool isCollisionInvulnerable = false;

        public class EquippedProjectileDataBaker : Baker<EquippedProjectileDataAuthoring>
        {
            public override void Bake(EquippedProjectileDataAuthoring authoring)
            {
                AddComponent<EquippedProjectileDataComponent>(new EquippedProjectileDataComponent{
                    active = authoring.active,
                    owner = GetEntity(authoring.owner),
                    activeVisual = GetEntity(authoring.spawnedVisual),
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