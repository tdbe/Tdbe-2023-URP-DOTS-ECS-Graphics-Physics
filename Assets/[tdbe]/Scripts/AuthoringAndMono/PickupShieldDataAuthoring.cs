using UnityEngine;
using Unity.Entities;

namespace GameWorld.Pickups
{
    public class PickupShieldDataAuthoring : MonoBehaviour
    {
        [Header("This component can be on a pickup or on a player/npc. \nIf it's on a player/npc then it represents the owner's default.")]
        public bool active = true;
        public GameObject activeVisual;
        public float timeToLive;
        public class PickupShieldBaker : Baker<PickupShieldDataAuthoring>
        {
            public override void Bake(PickupShieldDataAuthoring authoring)
            {
                AddComponent<PickupShieldDataComponent>(new PickupShieldDataComponent{
                   active = authoring.active,
                   activeVisual = GetEntity(authoring.activeVisual),
                   timeToLive = authoring.timeToLive
                });
            }
        }
    }

    
}