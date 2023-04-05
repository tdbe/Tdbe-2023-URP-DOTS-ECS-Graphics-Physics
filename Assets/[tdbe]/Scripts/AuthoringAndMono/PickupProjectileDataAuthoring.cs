using UnityEngine;
using Unity.Entities;

namespace GameWorld.Pickups
{
    public class PickupProjectileDataAuthoring : MonoBehaviour
    {
        [Header("This component can be on a pickup or on a player/npc. \nIf it's on a player/npc then it represents the default projectile.")]
        public bool active = true;
        [Header("Spawned active visual on owner, e.g. gun.")]
        public GameObject activeVisual;
        [Header("This is data for the projectile you shoot:")]
        public GameObject prefab;
        [Header("This is the projectile's TTL.")]
        public float timeToLive;
        public float speed = 1;
        public float scale = 1;
        //public double pickupTime = 0;
        [Header("This is the TTL of this pickup in the owner's equipped pickup slot.")]
        public double pickupTimeToLive = 1;
        [Header("This will add an Invulnerable tag to the spawned prefab.")]
        public bool isCollisionInvulnerable;
        public class PickupProjectileBaker : Baker<PickupProjectileDataAuthoring>
        {
            public override void Bake(PickupProjectileDataAuthoring authoring)
            {
                AddComponent<PickupProjectileDataComponent>(new PickupProjectileDataComponent{
                   active = authoring.active,
                   activeVisual = GetEntity(authoring.activeVisual),
                   prefab = GetEntity(authoring.prefab),
                   timeToLive = authoring.timeToLive,
                   speed = authoring.speed,
                   scale = authoring.scale,
                   //pickupTime = authoring.pickupTime,
                   pickupTimeToLive = authoring.pickupTimeToLive,
                   isCollisionInvulnerable = authoring.isCollisionInvulnerable
                });
            }
        }
    }

    
}