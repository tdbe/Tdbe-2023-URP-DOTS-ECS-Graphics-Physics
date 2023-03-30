using UnityEngine;
using Unity.Entities;

namespace GameWorld.Pickups
{
    public class PickupProjectileDataAuthoring : MonoBehaviour
    {
        [Header("This component can be on a pickup or on a player/npc. \nIf it's on a player/npc then it represents the default projectile.")]
        public GameObject activeVisual;
        public GameObject prefab;
        public float timeToLive;
        public bool isCollisionInvulnerable;
        public class PickupProjectileBaker : Baker<PickupProjectileDataAuthoring>
        {
            public override void Bake(PickupProjectileDataAuthoring authoring)
            {
                AddComponent<PickupProjectileDataComponent>(new PickupProjectileDataComponent{
                   activeVisual = GetEntity(authoring.activeVisual),
                   prefab = GetEntity(authoring.prefab),
                   timeToLive = authoring.timeToLive,
                   isCollisionInvulnerable = authoring.isCollisionInvulnerable
                });
            }
        }
    }

    
}