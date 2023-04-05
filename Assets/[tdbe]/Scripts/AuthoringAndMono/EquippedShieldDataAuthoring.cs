using UnityEngine;
using Unity.Entities;

namespace GameWorld.Pickups
{
    public class EquippedShieldDataAuthoring : MonoBehaviour
    {
        [Header("This is the equipment slot for the pickup data. \nThe Shield pickup data goes in this slot. \nStores the data from pickups or \nfrom a default 'pickup' on the owner if available.")]
        public bool active = false;
        [Header("Owner, e.g. player.")]
        public GameObject owner;
        [HideInInspector]
        [Header("Spawned active visual on owner, e.g. gun.")]
        public GameObject activeVisual;
        [HideInInspector]
        public double pickupTime = 0;
        [HideInInspector]
        public double pickupTimeToLive = 0;
        
        public class EquippedShieldDataBaker : Baker<EquippedShieldDataAuthoring>
        {
            public override void Bake(EquippedShieldDataAuthoring authoring)
            {
                AddComponent<EquippedShieldDataComponent>(new EquippedShieldDataComponent{
                    active = authoring.active,
                    owner = GetEntity(authoring.owner),
                    activeVisual = GetEntity(authoring.activeVisual),
                    pickupTime = authoring.pickupTime,
                    pickupTimeToLive = authoring.pickupTimeToLive
                });
            }
        }
    }

    
}