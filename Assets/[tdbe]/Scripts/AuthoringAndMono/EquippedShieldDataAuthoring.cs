using UnityEngine;
using Unity.Entities;

namespace GameWorld.Pickups
{
    public class EquippedShieldDataAuthoring : MonoBehaviour
    {
        [Header("This is the shield data that gets read by the system.")]
        public bool active = false;
        [Header("e.g. player.")]
        public GameObject owner;
        [Header("Spawned active visual on owner, e.g. gun.")]
        public GameObject spawnedVisual;
        public double pickupTime = 0;
        public double pickupTimeToLive = 1;
        
        public class EquippedShieldDataBaker : Baker<EquippedShieldDataAuthoring>
        {
            public override void Bake(EquippedShieldDataAuthoring authoring)
            {
                AddComponent<EquippedShieldDataComponent>(new EquippedShieldDataComponent{
                    active = authoring.active,
                    owner = GetEntity(authoring.owner),
                    spawnedVisual = GetEntity(authoring.spawnedVisual),
                    pickupTime = authoring.pickupTime,
                    pickupTimeToLive = authoring.pickupTimeToLive
                });
            }
        }
    }

    
}